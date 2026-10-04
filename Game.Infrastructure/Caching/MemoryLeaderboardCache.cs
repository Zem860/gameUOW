using Game.Abstractions.Caching;
using Game.Abstractions.DependencyInjection;
using Game.Abstractions.Dtos.Leaderboard;
using Microsoft.Extensions.Caching.Memory;

namespace Game.Infrastructure.Caching
{
    /// <summary>
    /// 以記憶體（IMemoryCache）實作的排行榜快取：Redis 故障時的備援。
    /// 每個遊戲只保留分數最高的前 <see cref="MaxEntries"/> 筆；程式重開就清空，只存在這台伺服器
    /// </summary>
    [ExposeServices(typeof(MemoryLeaderboardCache))]
    public class MemoryLeaderboardCache : ILeaderboardCache, ISingletonDependency
    {
        /// <summary>
        /// 每個遊戲最多保留的筆數（排行榜只顯示前幾名，不需要全部）
        /// </summary>
        public const int MaxEntries = 100;
        private readonly IMemoryCache _memoryCache;
        // 同一份清單可能被多個 Request 同時修改，用鎖確保一次只有一個人改
        private readonly Lock _lock = new();

        public MemoryLeaderboardCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 加入一筆成績：放進清單後重新排序（分數高→低），超過上限的尾端捨棄。
        /// 同一個 gameResultId 重複加入時只會保留一筆（冪等）
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="entry">成績</param>
        public Task AddAsync(string gameId, LeaderboardEntry entry)
        {
            lock (_lock)
            {
                IReadOnlyList<LeaderboardEntry> current = GetEntries(gameId);
                // 每次都建一份新清單再整份換掉，不直接改舊清單：
                // 正在讀舊清單的人不會讀到改到一半的資料
                List<LeaderboardEntry> updated = current
                // 先移除同一個 gameResultId 的舊資料，再加入新的，避免同一局出現兩次。
                // 競態條件：存檔是「先寫 Mongo、再寫快取」，若有人在這兩步之間看排行榜，
                // 會因快取是空的而從 Mongo 補回快取（已含這一局），接著這裡又加一次。
                // lock 擋不住這種「排隊各加一次」的重複，要靠這行讓加兩次的結果等於加一次
                .Where(item => item.GameResultId != entry.GameResultId)
                // 新的一筆要在排序之前加入，才會一起參與排序
                .Append(entry)
                .OrderByDescending(item => item.Score)
                .Take(MaxEntries)
                // 前面每一步都只是串起規則，到 ToList 才真的執行並產生清單
                .ToList();
                _memoryCache.Set(CacheKey(gameId), (IReadOnlyList<LeaderboardEntry>)updated);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 取得分數最高的前 N 筆（清單本來就由高到低排好，直接取前面）
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="count">筆數</param>
        /// <returns>前 N 筆成績；快取沒有資料時回傳空集合</returns>
        public Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string gameId, int count)
        {
            IReadOnlyList<LeaderboardEntry> top = GetEntries(gameId).Take(count).ToList();
            return Task.FromResult(top);
        }

        /// <summary>
        /// 從記憶體取出某個遊戲的整份清單；還沒有資料時回傳空集合
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <returns>該遊戲的整份清單（分數由高到低）</returns>
        private IReadOnlyList<LeaderboardEntry> GetEntries(string gameId) =>
        _memoryCache.TryGetValue(CacheKey(gameId), out IReadOnlyList<LeaderboardEntry>? entries)
        && entries is not null
        ? entries
        : Array.Empty<LeaderboardEntry>();

        private static string CacheKey(string gameId) => $"Leaderboard:{gameId}";
    }

}