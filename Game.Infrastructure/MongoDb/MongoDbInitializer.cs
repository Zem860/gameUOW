using Game.Abstractions.DependencyInjection;
using Game.Abstractions.Initialization;
using Game.Domain.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Game.Infrastructure.MongoDb
{
    /// <summary>
    /// MongoDB 初始化：建立索引與種子資料
    /// </summary>
    /// <remarks>
    /// 使用位置：Game.Web 的 InitializeDatabaseAsync() 在啟動時呼叫一次。
    /// 可以重複執行：索引定義相同時 Mongo 不會重建；種子資料用 upsert，已存在就不動。
    /// 修改既有索引的定義（欄位、選項）時，要先到 Atlas 刪掉舊索引，否則會發生衝突、啟動失敗。
    /// </remarks>
    public class MongoDbInitializer : IDatabaseInitializer, IScopedDependency
    {
        /// <summary>第一款遊戲的代碼</summary>
        private const string SnakeCode = "snake";

        private readonly MongoDbContext _context;
        private readonly ILogger<MongoDbInitializer> _logger;

        public MongoDbInitializer(MongoDbContext context, ILogger<MongoDbInitializer> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await CreateIndexesAsync(cancellationToken);
            await SeedGamesAsync(cancellationToken);
        }

        /// <summary>
        /// 建立索引；失敗時拋出例外讓啟動失敗
        /// </summary>
        private async Task CreateIndexesAsync(CancellationToken cancellationToken)
        {
            // games.code 唯一：同一個遊戲代碼只能有一筆
            await _context.Games.Indexes.CreateOneAsync(
                new CreateIndexModel<GameInfo>(
                    Builders<GameInfo>.IndexKeys.Ascending(game => game.Code),
                    new CreateIndexOptions { Name = "ux_code", Unique = true }),
                cancellationToken: cancellationToken);

            await _context.GameResults.Indexes.CreateManyAsync(
                [
                    // nonce 唯一：同一張票券只能兌換一次（第二層防線，主防線是 _id）
                    new CreateIndexModel<GameResult>(
                          Builders<GameResult>.IndexKeys.Ascending(result => result.Nonce),
                          new CreateIndexOptions { Name = "ux_nonce", Unique = true }),

                      // 排行榜查詢：依遊戲篩選，分數高到低，同分時先完成的在前
                      new CreateIndexModel<GameResult>(
                          Builders<GameResult>.IndexKeys
                              .Ascending(result => result.GameId)
                              .Descending(result => result.Score)
                              .Ascending(result => result.FinishedAt),
                          new CreateIndexOptions { Name = "ix_gameId_score_finishedAt" }),
                  ],
                cancellationToken);

            _logger.LogInformation("MongoDB 索引檢查完成");
        }

        /// <summary>
        /// 建立種子遊戲；用 upsert 一次完成「不存在才新增」，避免先查再寫的競爭問題
        /// </summary>
        private async Task SeedGamesAsync(CancellationToken cancellationToken)
        {
            UpdateResult result = await _context.Games.UpdateOneAsync(
                game => game.Code == SnakeCode,
                Builders<GameInfo>.Update
                    .SetOnInsert(game => game.DisplayName, "Snake")
                    .SetOnInsert(game => game.IsActive, true)
                    .SetOnInsert(game => game.CreatedAt, DateTimeOffset.UtcNow),
                new UpdateOptions { IsUpsert = true },
                cancellationToken);

            if (result.UpsertedId is not null)
            {
                _logger.LogInformation("已新增種子遊戲 {Code}", SnakeCode);
            }
        }
    }
}