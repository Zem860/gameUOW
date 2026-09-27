using Game.Abstractions.DependencyInjection;
using Game.Abstractions.Repositories;
using Game.Domain.Entities;
using MongoDB.Driver;

namespace Game.Infrastructure.MongoDb.Queries
{
    /// <summary>
    /// 遊戲結果的專用查詢；查詢走讀取節點（secondary）
    /// </summary>
    /// <remarks>
    /// 使用位置：LeaderboardService 在 Redis 故障、記憶體快取也沒有資料時，改從 MongoDB 取排行榜。
    /// 對應索引：gameResults { gameId: 1, score: -1, finishedAt: 1 }（啟動時建立）。
    /// </remarks>
    public class GameResultQueryExecutor : IGameResultQueryExecutor, IScopedDependency
    {
        private readonly MongoDbContext _context;

        public GameResultQueryExecutor(MongoDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<GameResult>> GetTopScoresAsync(
            string gameId,
            int count,
            CancellationToken cancellationToken = default)
        {
            return await _context.GameResults
                .WithReadPreference(ReadPreference.SecondaryPreferred)
                .Find(result => result.GameId == gameId)
                .SortByDescending(result => result.Score)
                .ThenBy(result => result.FinishedAt)   // 同分時，先達成的排前面
                .Limit(count)
                .ToListAsync(cancellationToken);
        }
    }
}
