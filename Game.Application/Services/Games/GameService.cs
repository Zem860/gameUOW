using System.Globalization;
using Game.Abstractions.DependencyInjection;
using Game.Abstractions.Dtos.Game;
using Game.Abstractions.IApplication;
using Game.Abstractions.IApplication.Security;
using Game.Abstractions.Repositories;
using Game.Application.Utils;
using Game.Domain.Entities;

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

        /// <summary>
        /// 建立遊戲流程服務
        /// </summary>
        /// <param name="gameRepository">遊戲唯讀 Repository</param>
        /// <param name="hmacService">HMAC 簽章服務</param>
        /// <param name="timeProvider">系統時鐘</param>
        public GameService(
            IReadRepository<GameInfo> gameRepository,
            IWriteRepository<GameResult> gameResultRepository,
            IUnitOfWork unitOfWork,
            IHmacService hmacService,
            TimeProvider timeProvider
            )
        {
            _gameRepository = gameRepository;
            _hmacService = hmacService;
            _timeProvider = timeProvider;
            _unitOfWork = unitOfWork;
            _gameResultRepository = gameResultRepository;
        }

        /// <inheritdoc />
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
            await _unitOfWork.ExecuteInTransactionAsync(
                token => _gameResultRepository.AddAsync(gameResult, token), cancellationToken
            );
            return new CreateGameResultResponse
            {
                GameResultId = gameResult.Id,
            };
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
