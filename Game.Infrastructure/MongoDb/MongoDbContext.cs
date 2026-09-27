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

        /// <summary>Entity 型別對應的 collection 名稱</summary>
        private static readonly Dictionary<Type, string> CollectionNames = new()
        {
            //字典最初的寫法，為了方便 GetCollection<TEntity>() 依型別取得 collection
            [typeof(GameInfo)] = GamesCollectionName,
            [typeof(GameResult)] = GameResultsCollectionName,
        };
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

        /// <summary>
        /// 依 Entity 型別取得對應的 collection
        /// </summary>
        /// <typeparam name="TEntity">Entity 型別</typeparam>
        /// <exception cref="InvalidOperationException">型別沒有登記在對照表時拋出</exception>
        public IMongoCollection<TEntity> GetCollection<TEntity>()
        {
            if (!CollectionNames.TryGetValue(typeof(TEntity), out string? collectionName))
            {
                throw new InvalidOperationException($"{typeof(TEntity).Name} 沒有對應的 collection");
            }
            //對應Dbcontext的DbSet<TEntity>，但MongoDB沒有DbSet，所以用GetCollection<TEntity>()取代
            return Database.GetCollection<TEntity>(collectionName);
        }
    }
}
