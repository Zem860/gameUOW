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
              // 不寫記憶體：記憶體只放資料庫查到的整份清單，逐筆加入會讓它變成不完整的清單。
              // 這筆成績已經在資料庫，記憶體過期後重新查詢就會出現
              catch (Exception ex)
              {
                  _logger.LogWarning(ex, "Redis 寫入排行榜失敗，成績已存入資料庫，稍後重新查詢時會出現。GameId={GameId}", gameId);
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

                  /// <summary>
          /// 整份放入：先寫 Redis，失敗時記錄警告並改放記憶體
          /// </summary>
          /// <param name="gameId">遊戲 Id</param>
          /// <param name="entries">完整清單（分數由高到低）</param>
          public async Task SetAllAsync(string gameId, IReadOnlyList<LeaderboardEntry> entries)
          {
              try
              {
                  await _redisCache.SetAllAsync(gameId, entries);
              }
              // 記憶體只在這裡寫入：資料一定是從資料庫查到的完整清單
              catch (Exception ex)
              {
                  _logger.LogWarning(ex, "Redis 寫入完整排行榜失敗，改放記憶體快取。GameId={GameId}", gameId);
                  await _memoryCache.SetAllAsync(gameId, entries);
              }
          }

    }
}