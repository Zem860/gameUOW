using Game.Abstractions.Caching;
using Game.Abstractions.DependencyInjection;
using Game.Abstractions.Dtos.Leaderboard;
using Game.Infrastructure.Redis;
using Microsoft.Extensions.Logging;

namespace Game.Infrastructure.Caching
{
    /// <summary>
    /// 排行榜快取的對外入口：優先使用 Redis，Redis 發生錯誤時改用記憶體快取。
    /// 呼叫端只依賴 ILeaderboardCache，不知道背後切換了哪一個實作
    /// </summary>
    public class FallbackLeaderboardCache : ILeaderboardCache, ISingletonDependency
    {
        private readonly RedisLeaderboardCache _redisCache;
        private readonly MemoryLeaderboardCache _memoryCache;

        private readonly ILogger<FallbackLeaderboardCache> _logger;

        // 注入「具體類別」而不是 ILeaderboardCache：
        // ILeaderboardCache 會拿到自己（FallbackLeaderboardCache），變成自己依賴自己
        public FallbackLeaderboardCache(RedisLeaderboardCache redisCache, MemoryLeaderboardCache memoryCache, ILogger<FallbackLeaderboardCache> logger)
        {
            _redisCache = redisCache;
            _memoryCache = memoryCache;
            _logger = logger;
        }
        /// <summary>
        /// 加入一筆成績：先寫 Redis，失敗時記錄警告並改寫記憶體
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="entry">成績</param>
        public async Task AddAsync(string gameId, LeaderboardEntry entry)
        {
            try
            {
                await _redisCache.AddAsync(gameId, entry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 寫入排行榜失敗，改寫記憶體快取。GameId={GameId}", gameId);
                await _memoryCache.AddAsync(gameId, entry);
            }
        }

        /// <summary>
        /// 取得分數最高的前 N 筆：先讀 Redis，失敗時記錄警告並改讀記憶體
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="count">筆數</param>
        /// <returns>前 N 筆成績；快取沒有資料時回傳空集合</returns>
        public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string gameId, int count)
        {
            try
            {
                return await _redisCache.GetTopAsync(gameId, count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 讀取排行榜失敗，改讀記憶體快取。GameId={GameId}", gameId);
                return await _memoryCache.GetTopAsync(gameId, count);
            }
        }

    }
}