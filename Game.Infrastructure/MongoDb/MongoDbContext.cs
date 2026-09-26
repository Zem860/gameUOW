using Game.Abstractions.DependencyInjection;
using Game.Domain.Entities;
using MongoDB.Driver;

namespace Game.Infrastructure.MongoDb
{
    /// <summary>
    /// MongoDB 存取入口：提供 Client、資料庫與各 collection
    /// </summary>
    /// <remarks>
    /// Singleton：MongoClient 內含連線池且執行緒安全，整個應用程式共用一個。
    /// Client 與 Database 由 AddMongoDbServices 註冊，這裡只向 DI 取用。
    /// Session 不放這裡，由 UnitOfWork（Scoped）每個 Request 各自開。
    /// </remarks>
    public class MongoDbContext : ISingletonDependency
    {
        /// <summary>games collection 名稱</summary>
        public const string GamesCollectionName = "games";

        /// <summary>gameResults collection 名稱</summary>
        public const string GameResultsCollectionName = "gameResults";

        public MongoDbContext(IMongoClient client, IMongoDatabase database)
        {
            Client = client;
            Database = database;
        }

        /// <summary>MongoClient（UnitOfWork 用它開 Session）</summary>
        public IMongoClient Client { get; }

        /// <summary>目前使用的資料庫</summary>
        public IMongoDatabase Database { get; }

        /// <summary>games collection</summary>
        public IMongoCollection<GameInfo> Games => Database.GetCollection<GameInfo>(GamesCollectionName);

        /// <summary>gameResults collection</summary>
        public IMongoCollection<GameResult> GameResults => Database.GetCollection<GameResult>(GameResultsCollectionName);
    }
}
