using Game.Abstractions.Caching;
using Game.Abstractions.Dtos.Leaderboard;
using Game.Abstractions.DependencyInjection;
using StackExchange.Redis;

namespace Game.Infrastructure.Redis
{
    /// <summary>
    /// 以 Redis 實作的排行榜快取：Sorted Set 存「gameResultId → 分數」，Hash 存「gameResultId → 玩家名稱」
    /// </summary>
    public class RedisLeaderboardCache : ILeaderboardCache, IScopedDependency
    {
        private readonly IConnectionMultiplexer _redis;
        public RedisLeaderboardCache(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        /// <inheritdoc />
        public async Task AddAsync(string gameId, LeaderboardEntry entry)
        {
            IDatabase db = _redis.GetDatabase();

            ITransaction transaction = db.CreateTransaction();
            // 這兩行只是「排進交易」，此時還沒送出；不可以 await，要等 ExecuteAsync 之後才會完成
            // HSET leaderboard:{gameId}:names {gameResultId} {playerName}

            // 這兩行只是「排進交易」，此時還沒送出；不可以 await，要等 ExecuteAsync 之後才會完成
            // HSET leaderboard:{gameId}:names {gameResultId} {playerName}
            _ = transaction.HashSetAsync(NamesKey(gameId), entry.GameResultId, entry.PlayerName);
            // ZADD leaderboard:{gameId} {score} {gameResultId}
            _ = transaction.SortedSetAddAsync(ScoresKey(gameId), entry.GameResultId, entry.Score);
            await transaction.ExecuteAsync();
        }


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




        private static string ScoresKey(string gameId) => $"leaderboard:{gameId}";

        private static string NamesKey(string gameId) => $"leaderboard:{gameId}:names";
    }
}