using System.Linq.Expressions;
using Game.Abstractions.Repositories;
using MongoDB.Driver;

namespace Game.Infrastructure.MongoDb.Repositories
{
    /// <summary>
    /// 唯讀 Repository 實作；查詢走讀取節點（secondary），讀取節點無法使用時自動改走主節點
    /// </summary>
    /// <typeparam name="TEntity">Entity 型別</typeparam>
    public class ReadRepository<TEntity> : IReadRepository<TEntity>
        where TEntity : class
    {
        /// <summary>Entity 的 Id 屬性名稱（用 C# 名稱，Driver 才會套用 class map 的型別轉換）</summary>
        private const string IdMemberName = "Id";

        private readonly MongoDbContext _context;

        public ReadRepository(MongoDbContext context)
        {
            _context = context;
        }

        /// <summary>指定讀取節點的 collection</summary>
        private IMongoCollection<TEntity> Collection =>
            _context.GetCollection<TEntity>().WithReadPreference(ReadPreference.SecondaryPreferred);

        /// <inheritdoc />
        public async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Collection
                    .Find(Builders<TEntity>.Filter.Eq(IdMemberName, id))
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (FormatException)
            {
                // Id 格式與儲存型別不符（例：ObjectId 收到非 24 碼 hex），視為找不到
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await Collection.Find(predicate).FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await Collection.Find(FilterDefinition<TEntity>.Empty).ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<List<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await Collection.Find(predicate).ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return (int)await Collection.CountDocumentsAsync(FilterDefinition<TEntity>.Empty, cancellationToken:
cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return (int)await Collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await Collection.Find(predicate).Limit(1).AnyAsync(cancellationToken);
        }
    }
}