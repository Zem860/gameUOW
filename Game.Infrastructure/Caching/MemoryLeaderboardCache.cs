using Game.Abstractions.DependencyInjection;
using Game.Abstractions.Dtos.Leaderboard;
using Microsoft.Extensions.Caching.Memory;

namespace Game.Infrastructure.Caching
{
    /// <summary>
    /// 以記憶體（IMemoryCache）暫存的排行榜：Redis 故障時的備援。
    /// 只存放從資料庫查到的整份清單，不逐筆累加；過期後由呼叫端重新查資料庫放入，
    /// 因此資料最多舊 <see cref="Expiration"/>，不會一直殘存。程式重開就清空，只存在這台伺服器
    /// </summary>
    public class MemoryLeaderboardCache : ISingletonDependency
    {
        /// <summary>
        /// 清單的存活時間：越短越即時，但 Redis 故障期間查資料庫的次數越多
        /// </summary>
        public static readonly TimeSpan Expiration = TimeSpan.FromSeconds(30);

        private readonly IMemoryCache _memoryCache;

        public MemoryLeaderboardCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 整份放入（覆蓋舊的），<see cref="Expiration"/> 後自動過期
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="entries">從資料庫查到的完整清單（分數由高到低）</param>
        public Task SetAllAsync(string gameId, IReadOnlyList<LeaderboardEntry> entries)
        {
            // 整份覆蓋，不在舊清單上修改：正在讀舊清單的人不受影響，也就不需要 lock
            _memoryCache.Set(CacheKey(gameId), entries, Expiration);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 取得分數最高的前 N 筆（清單本來就由高到低排好，直接取前面）
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="count">筆數</param>
        /// <returns>前 N 筆成績；沒有資料或已過期時回傳空集合</returns>
        public Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string gameId, int count)
        {
            if (!_memoryCache.TryGetValue(CacheKey(gameId), out IReadOnlyList<LeaderboardEntry>? entries)
                || entries is null)
            {
                return Task.FromResult<IReadOnlyList<LeaderboardEntry>>(Array.Empty<LeaderboardEntry>());
            }

            IReadOnlyList<LeaderboardEntry> top = entries.Take(count).ToList();
            return Task.FromResult(top);
        }

        private static string CacheKey(string gameId) => $"leaderboard:{gameId}";
    }
}