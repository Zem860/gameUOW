using Game.Abstractions.Caching;
using Game.Abstractions.DependencyInjection;
using Game.Abstractions.Dtos.Leaderboard;
using Game.Abstractions.IApplication;
using Game.Abstractions.Repositories;
using Game.Domain.Entities;

namespace Game.Application.Services.Leaderboards
{
    /// <summary>
    /// 排行榜服務：快取優先，快取沒有資料時從資料庫查詢並補回快取
    /// </summary>
    [ExposeServices(typeof(ILeaderboardService))]
    public class LeaderboardService : ILeaderboardService, IScopedDependency
    {
        /// <summary>
        /// 單次查詢的筆數上限；從資料庫補回快取時也一次補這麼多，
        /// 之後要看較多名次的請求才不會因為快取「有資料但筆數不夠」而拿不到
        /// </summary>
        public const int MaxCount = 100;

        // 只有讀取、不寫入 Mongo，所以不需要 IUnitOfWork：
        // UoW 只負責寫入的交易；讀取不改資料，不需要交易，
        // 而且交易只能在 primary 執行，讀取包進交易就無法分流到 secondary
        // （補回快取寫的是 Redis / 記憶體，不是 Mongo）

        // 簡單的「找一筆」：泛型唯讀 Repository 就做得到（code → gameId）
        private readonly IReadRepository<GameInfo> _gameRepository;
        // 排行榜要「排序 + 限制筆數」，泛型 Repository 沒有這兩個功能，改用專用查詢
        private readonly IGameResultQueryExecutor _gameResultQueryExecutor;
        private readonly ILeaderboardCache _leaderboardCache;
        /// <summary>
        /// 建立排行榜服務
        /// </summary>
        /// <param name="gameRepository">遊戲唯讀 Repository</param>
        /// <param name="gameResultQueryExecutor">遊戲結果專用查詢</param>
        /// <param name="leaderboardCache">排行榜快取</param>
        public LeaderboardService(IReadRepository<GameInfo> gameRepository, IGameResultQueryExecutor gameResultQueryExecutor, ILeaderboardCache leaderboardCache)
        {
            _gameRepository = gameRepository;
            _gameResultQueryExecutor = gameResultQueryExecutor;
            _leaderboardCache = leaderboardCache;
        }
        /// <summary>
        /// 取得指定遊戲分數最高的前 N 筆：先讀快取，快取沒有資料時從資料庫查詢並補回快取
        /// </summary>
        /// <param name="code">遊戲代碼（例如 snake）</param>
        /// <param name="count">筆數（1～100）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>前 N 筆成績（分數由高到低）；沒有任何成績時回傳空集合</returns>
        /// <exception cref="ArgumentException">筆數不在 1～100 之間</exception>
        /// <exception cref="KeyNotFoundException">遊戲不存在或已停用</exception>
        public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string code, int count, CancellationToken cancellationToken = default)
        {
            if (count < 1 || count > MaxCount)
            {
                throw new ArgumentException($"筆數不在 1～{MaxCount} 之間", nameof(count));
            }

            // ① 遊戲代碼 → gameId（快取與資料庫都是用 gameId 分開存）

            GameInfo? game = await _gameRepository.FirstOrDefaultAsync(g => g.Code == code && g.IsActive, cancellationToken);
            if (game == null)
            {
                throw new KeyNotFoundException($"遊戲不存在或已停用：{code}");
            }
            // ② 先讀快取：ILeaderboardCache.GetTopAsync 只讀快取、不碰資料庫；
            //    實際是 Fallback 版（先讀 Redis，失敗改讀記憶體），這裡不用管是哪一個

            IReadOnlyList<LeaderboardEntry> cached = await _leaderboardCache.GetTopAsync(game.Id, count);
            if (cached != null && cached.Count > 0)
            {
                return cached;
            }
            // ③ 快取是空的（剛啟動、Redis 被清空、或改用記憶體備援）→ 從資料庫一次查上限筆數；
            //    整個流程只有這裡會讀資料庫（GetTopScoresAsync），其他 GetTopAsync 都只讀快取
            IReadOnlyList<GameResult> results = await _gameResultQueryExecutor.GetTopScoresAsync(game.Id, MaxCount, cancellationToken);
            List<LeaderboardEntry> entries = results.Select(result => new LeaderboardEntry
            {
                GameResultId = result.Id,
                PlayerName = result.PlayerName,
                Score = result.Score,
            }).ToList();

            // ④ 補回快取，下一個請求就不用再查資料庫

            foreach (LeaderboardEntry entry in entries)
            {
                await _leaderboardCache.AddAsync(game.Id, entry);
            }
            // ⑤ 補的是上限筆數，回傳時只取請求要的筆數
            return entries.Take(count).ToList();
        }
    }

}