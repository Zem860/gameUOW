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
    /// <remarks>
    /// 斷路器：Redis 失敗一次後，<see cref="BreakDuration"/> 內所有操作直接略過 Redis，
    /// 避免每個請求都要等 Redis 逾時；時間到後下一個請求再試一次 Redis，成功就恢復
    /// </remarks>
    public class FallbackLeaderboardCache : ILeaderboardCache, ISingletonDependency
    {
        /// <summary>
        /// Redis 失敗後暫停使用的時間
        /// </summary>
        public static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(30);

        private readonly RedisLeaderboardCache _redisCache;
        private readonly MemoryLeaderboardCache _memoryCache;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<FallbackLeaderboardCache> _logger;

        // 在這個時間點之前不使用 Redis（UTC Ticks；0 = Redis 正常）。
        // 用 long 而不是 DateTimeOffset：多個請求同時讀寫時，搭配 Volatile 能保證讀到完整的值
        private long _redisPausedUntilTicks;

        // 注入「具體類別」而不是 ILeaderboardCache：
        // ILeaderboardCache 會拿到自己（FallbackLeaderboardCache），變成自己依賴自己
        public FallbackLeaderboardCache(
            RedisLeaderboardCache redisCache,
            MemoryLeaderboardCache memoryCache,
            TimeProvider timeProvider,
            ILogger<FallbackLeaderboardCache> logger)
        {
            _redisCache = redisCache;
            _memoryCache = memoryCache;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        /// <summary>
        /// 加入一筆成績：只寫 Redis；Redis 暫停中或失敗時略過，不寫記憶體
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="entry">成績</param>
        public async Task AddAsync(string gameId, LeaderboardEntry entry)
        {
            // 不寫記憶體：記憶體只放資料庫查到的整份清單，逐筆加入會讓它變成不完整的清單。
            // 這筆成績已經在資料庫，記憶體過期後重新查詢就會出現
            if (!IsRedisAvailable())
            {
                return;
            }

            try
            {
                await _redisCache.AddAsync(gameId, entry);
            }
            catch (Exception ex)
            {
                PauseRedis(ex, "寫入一筆成績", gameId);
            }
        }

        /// <summary>
        /// 取得分數最高的前 N 筆：先讀 Redis；Redis 暫停中或失敗時改讀記憶體
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="count">筆數</param>
        /// <returns>前 N 筆成績；快取沒有資料時回傳空集合</returns>
        public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string gameId, int count)
        {
            if (IsRedisAvailable())
            {
                try
                {
                    return await _redisCache.GetTopAsync(gameId, count);
                }
                catch (Exception ex)
                {
                    PauseRedis(ex, "讀取排行榜", gameId);
                }
            }

            return await _memoryCache.GetTopAsync(gameId, count);
        }

        /// <summary>
        /// 整份放入：先寫 Redis；Redis 暫停中或失敗時改放記憶體
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="entries">完整清單（分數由高到低）</param>
        public async Task SetAllAsync(string gameId, IReadOnlyList<LeaderboardEntry> entries)
        {
            if (IsRedisAvailable())
            {
                try
                {
                    await _redisCache.SetAllAsync(gameId, entries);
                    return;
                }
                catch (Exception ex)
                {
                    PauseRedis(ex, "寫入完整排行榜", gameId);
                }
            }

            // 記憶體只在這裡寫入：資料一定是從資料庫查到的完整清單
            await _memoryCache.SetAllAsync(gameId, entries);
        }

        /// <summary>
        /// 目前是否可以使用 Redis（不在暫停期間內）
        /// </summary>
        private bool IsRedisAvailable()
        {
            long nowTicks = _timeProvider.GetUtcNow().UtcTicks;
            return nowTicks >= Volatile.Read(ref _redisPausedUntilTicks);
        }

        /// <summary>
        /// Redis 失敗：記錄警告，並在 <see cref="BreakDuration"/> 內暫停使用 Redis
        /// </summary>
        /// <param name="ex">Redis 拋出的例外</param>
        /// <param name="action">失敗的動作（寫進 log 用）</param>
        /// <param name="gameId">遊戲 Id</param>
        private void PauseRedis(Exception ex, string action, string gameId)
        {
            DateTimeOffset pausedUntil = _timeProvider.GetUtcNow().Add(BreakDuration);
            Volatile.Write(ref _redisPausedUntilTicks, pausedUntil.UtcTicks);
            _logger.LogWarning(ex, "Redis {Action}失敗，{Seconds} 秒內改用記憶體快取。GameId={GameId}",
                action, BreakDuration.TotalSeconds, gameId);
        }
    }
}
