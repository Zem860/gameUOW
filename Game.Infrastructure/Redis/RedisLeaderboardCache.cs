using Game.Abstractions.Caching;
using Game.Abstractions.Dtos.Leaderboard;
using Game.Abstractions.DependencyInjection;
using StackExchange.Redis;

namespace Game.Infrastructure.Redis
{
    /// <summary>
    /// 以 Redis 實作的排行榜快取：Sorted Set 存「gameResultId → 分數」，Hash 存「gameResultId → 玩家名稱」
    /// </summary>
    [ExposeServices(typeof(RedisLeaderboardCache))]
    public class RedisLeaderboardCache : ILeaderboardCache, ISingletonDependency
    {
        private readonly IConnectionMultiplexer _redis;
        public RedisLeaderboardCache(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        /// <summary>
        /// 加入一筆成績：以交易（MULTI / EXEC）同時寫入名字（Hash）與分數（Sorted Set）
        /// </summary>
        /// <param name="gameId">遊戲 Id（組成 key：leaderboard:{gameId}）</param>
        /// <param name="entry">成績（gameResultId、玩家名稱、分數）</param>
        public async Task AddAsync(string gameId, LeaderboardEntry entry)
        {
            IDatabase db = _redis.GetDatabase();

            // MULTI：名字與分數放在同一個交易，一起執行，不會只寫進一半
            ITransaction transaction = db.CreateTransaction();

            // 這兩行只是「排進交易」，此時還沒送出；不可以 await，要等 ExecuteAsync 之後才會完成
            // HSET leaderboard:{gameId}:names {gameResultId} {playerName}
            _ = transaction.HashSetAsync(NamesKey(gameId), entry.GameResultId, entry.PlayerName);
            // ZADD leaderboard:{gameId} {score} {gameResultId}
            _ = transaction.SortedSetAddAsync(ScoresKey(gameId), entry.GameResultId, entry.Score);

            // EXEC：一次送出並執行
            await transaction.ExecuteAsync();
        }

        /// <summary>
        /// 取得分數最高的前 N 筆：先從 Sorted Set 取 id 與分數（由高到低），再從 Hash 一次取回名字
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="count">筆數</param>
        /// <returns>前 N 筆成績；快取沒有資料時回傳空集合</returns>
        public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string gameId, int count)
        {
            IDatabase db = _redis.GetDatabase();
            SortedSetEntry[] top = await db.SortedSetRangeByRankWithScoresAsync
            (ScoresKey(gameId), 0, count - 1, Order.Descending);
            if (top.Length == 0)
            {
                return Array.Empty<LeaderboardEntry>();
            }

            RedisValue[] ids = top.Select(item => item.Element).ToArray();
            RedisValue[] names = await db.HashGetAsync(NamesKey(gameId), ids);
            return top.Select((item, idx) => new LeaderboardEntry
            {
                GameResultId = item.Element.ToString(),
                PlayerName = names[idx].ToString(),
                Score = (int)item.Score,
            }).ToList();
        }

        /// <summary>
        /// 整份放入：以交易先刪掉舊的分數與名字，再一次寫入整份清單
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="entries">完整清單（分數由高到低）</param>
        public async Task SetAllAsync(string gameId, IReadOnlyList<LeaderboardEntry> entries)
        {
            // 沒有任何成績就不寫：保持空的，下次讀取再查資料庫
            if (entries.Count == 0)
            {
                return;
            }

            IDatabase db = _redis.GetDatabase();
            ITransaction transaction = db.CreateTransaction();

            // DEL：先清掉舊資料，避免新舊混在一起
            _ = transaction.KeyDeleteAsync(ScoresKey(gameId));
            _ = transaction.KeyDeleteAsync(NamesKey(gameId));

            // ZADD / HSET 一次帶多筆，只需要一個指令
            SortedSetEntry[] scores = entries
                .Select(entry => new SortedSetEntry(entry.GameResultId, entry.Score))
                .ToArray();
            HashEntry[] names = entries
                .Select(entry => new HashEntry(entry.GameResultId, entry.PlayerName))
                .ToArray();
            _ = transaction.SortedSetAddAsync(ScoresKey(gameId), scores);
            _ = transaction.HashSetAsync(NamesKey(gameId), names);

            await transaction.ExecuteAsync();
        }

        private static string ScoresKey(string gameId) => $"leaderboard:{gameId}";

        private static string NamesKey(string gameId) => $"leaderboard:{gameId}:names";
    }
}