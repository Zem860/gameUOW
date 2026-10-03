using System.Globalization;
using Game.Abstractions.DependencyInjection;
using Game.Abstractions.Dtos.Game;
using Game.Abstractions.IApplication;
using Game.Abstractions.IApplication.Security;
using Game.Abstractions.Repositories;
using Game.Application.Utils;
using Game.Domain.Entities;
using Game.Abstractions.Caching;
using Game.Abstractions.Dtos.Leaderboard;
using Microsoft.Extensions.Logging;

namespace Game.Application.Services.Games
{
    /// <summary>
    /// 遊戲流程服務
    /// </summary>
    [ExposeServices(typeof(IGameService))]
    public sealed class GameService : IGameService, IScopedDependency
    {
        private readonly IReadRepository<GameInfo> _gameRepository;
        private readonly IHmacService _hmacService;
        private readonly TimeProvider _timeProvider;
        private readonly IWriteRepository<GameResult> _gameResultRepository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILeaderboardCache _leaderboardCache;
        private readonly ILogger<GameService> _logger;

        /// <summary>
        /// 建立遊戲流程服務
        /// </summary>
        /// <param name="gameRepository">遊戲唯讀 Repository</param>
        /// <param name="gameResultRepository">遊戲結果寫入 Repository</param>
        /// <param name="unitOfWork">工作單元（交易）</param>
        /// <param name="hmacService">HMAC 簽章服務</param>
        /// <param name="timeProvider">系統時鐘</param>
        /// <param name="leaderboardCache">排行榜快取</param>
        /// <param name="logger">記錄器</param>
        public GameService(
            IReadRepository<GameInfo> gameRepository,
            IWriteRepository<GameResult> gameResultRepository,
            IUnitOfWork unitOfWork,
            IHmacService hmacService,
            TimeProvider timeProvider,
            ILeaderboardCache leaderboardCache,
            ILogger<GameService> logger
            )
        {
            _gameRepository = gameRepository;
            _hmacService = hmacService;
            _timeProvider = timeProvider;
            _unitOfWork = unitOfWork;
            _gameResultRepository = gameResultRepository;
            _leaderboardCache = leaderboardCache;
            _logger = logger;
        }

        /// <summary>
        /// 開始遊戲：確認遊戲存在且啟用，簽發遊戲票券（不寫入資料庫）
        /// </summary>
        /// <param name="code">遊戲代碼（例如 snake）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>遊戲票券（gameResultId、gameId、startedAt、nonce、signature）</returns>
        /// <exception cref="KeyNotFoundException">遊戲不存在或已停用</exception>
        public async Task<StartGameResponse> StartAsync(string code, CancellationToken cancellationToken = default)
        {
            GameInfo? game = await _gameRepository.FirstOrDefaultAsync(game => game.Code == code && game.IsActive, cancellationToken);
            if (game is null)
            {
                throw new KeyNotFoundException($"game not found: {code}");
            }

            // gameResultId 每張票都不同，存檔時成為 _id，是防重複送出的主防線；nonce 是第二層
            string gameResultId = Guid.NewGuid().ToString();
            long startedAt = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
            string nonce = NonceGenerator.Generate();

            // 簽章只證明「伺服器發的、沒被改過」；票券內容是公開的，不做加密
            string signature = _hmacService.Sign(BuildTicketPayload(gameResultId, game.Id, startedAt, nonce));

            return new StartGameResponse
            {
                GameResultId = gameResultId,
                GameId = game.Id,
                StartedAt = startedAt,
                Nonce = nonce,
                Signature = signature,
            };
        }

        /// <summary>
        /// 儲存遊戲結果：驗證票券 → 檢查名字與分數 → 確認遊戲仍啟用 → 寫入資料庫 → 更新排行榜快取
        /// </summary>
        /// <param name="request">票券 + 玩家名字 + 分數</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>已儲存的遊戲結果識別碼</returns>
        /// <exception cref="UnauthorizedAccessException">票券簽章不符（被竄改或偽造）</exception>
        /// <exception cref="ArgumentException">名字全為空白，或分數為負數</exception>
        /// <exception cref="KeyNotFoundException">遊戲不存在或已停用</exception>
        public async Task<CreateGameResultResponse> FinishAsync(
            CreateGameResultRequest request,
            CancellationToken cancellationToken
        )
        {
            // ① 票券是否為伺服器簽發且未被竄改
            VerifyTicket(request);
            // ② 名字：前後空白不算，不能全是空白
            string playerName = request.PlayerName.Trim();
            if (playerName.Length == 0)
            {
                throw new ArgumentException("玩家名字不可為空白");
            }
            // 分數合理性規則之後再補，目前只擋負數
            if (request.Score < 0)
            {
                throw new ArgumentException("分數不可為負數");
            }
            // ③ 遊戲可能在發票之後被停用
            bool gameExist = await _gameRepository.AnyAsync(game => game.Id == request.GameId && game.IsActive, cancellationToken);
            if (!gameExist)
            {
                throw new KeyNotFoundException($"game not found: {request.GameId}");
            }
            DateTimeOffset now = _timeProvider.GetUtcNow();
            GameResult gameResult = new()
            {
                Id = request.GameResultId,
                GameId = request.GameId,
                PlayerName = playerName,
                Score = request.Score,
                StartedAt = DateTimeOffset.FromUnixTimeSeconds(request.StartedAt),
                FinishedAt = now,
                Nonce = request.Nonce,
                CreatedAt = now,
            };

            // 寫入交給 UoW 的交易：成功自動 Commit、例外自動 Rollback、暫時性錯誤會重跑整個 lambda。
            // token：Driver 呼叫 lambda 時給的取消權杖（源自外面的 cancellationToken），照規矩往下傳給 AddAsync，
            //        玩家中途斷線時 Mongo 的寫入就會停止等待；
            // gameResult：lambda 從外面「借」來的變數（closure），所以參數只需要 token
            await _unitOfWork.ExecuteInTransactionAsync(
                token => _gameResultRepository.AddAsync(gameResult, token), cancellationToken
            );

            // ④ 更新排行榜快取：放在交易完成之後（交易可能重試，放在裡面 Redis 會被寫好幾次）
            await AddToLeaderboardAsync(gameResult);

            return new CreateGameResultResponse
            {
                GameResultId = gameResult.Id,
            };
        }

        /// <summary>
        /// 將成績寫入排行榜快取；快取失敗只記 warning，不影響存檔結果（Mongo 才是真相來源）
        /// </summary>
        /// <param name="gameResult">已存進 Mongo 的遊戲結果</param>
        private async Task AddToLeaderboardAsync(GameResult gameResult)
        {
            try
            {
                await _leaderboardCache.AddAsync(gameResult.GameId, new LeaderboardEntry
                {
                    GameResultId = gameResult.Id,
                    PlayerName = gameResult.PlayerName,
                    Score = gameResult.Score,
                });
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "排行榜快取寫入失敗，GameResultId：{GameResultId}", gameResult.Id);
            }
        }

        /// <summary>
        /// 驗證票券：用收到的四個欄位重組簽章內容再比對；不符代表票券被竄改或偽造
        /// </summary>
        /// <param name="request">存檔請求（含票券）</param>
        /// <exception cref="UnauthorizedAccessException">簽章不符時拋出</exception>
        private void VerifyTicket(CreateGameResultRequest request)
        {
            string payload = BuildTicketPayload(
                  request.GameResultId,
                  request.GameId,
                  request.StartedAt,
                  request.Nonce);
            var match = _hmacService.Verify(payload, request.Signature);
            if (!match)
            {
                throw new UnauthorizedAccessException("票券驗證失敗");
            }
        }

        /// <summary>
        /// 組出票券的簽章內容；簽發與驗證必須用同一個方法，欄位順序與格式才會一致
        /// </summary>
        private static string BuildTicketPayload(string gameResultId, string gameId, long startedAt, string nonce)
        {
            return string.Join(
                '|',
                gameResultId,
                gameId,
                startedAt.ToString(CultureInfo.InvariantCulture),
                nonce);
        }
    }
}
