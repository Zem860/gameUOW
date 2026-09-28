using System.Linq.Expressions;
using Game.Abstractions.Exceptions;
using Game.Abstractions.Repositories;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Game.Infrastructure.MongoDb.Repositories
{
    /// <summary>
    /// 寫入 Repository 實作；寫入主節點（primary）
    /// </summary>
    /// <remarks>
    /// <see cref="MongoSessionAccessor"/> 有 Session 時帶著 Session 寫入（加入交易），沒有時直接寫入。
    /// </remarks>
    /// <typeparam name="TEntity">Entity 型別</typeparam>
    public class WriteRepository<TEntity> : IWriteRepository<TEntity>
        where TEntity : class
    {
        /// <summary>Entity 的 Id 屬性名稱（用 C# 名稱，Driver 才會套用 class map 的型別轉換）</summary>
        private const string IdMemberName = "Id";

        private readonly MongoDbContext _context;
        private readonly MongoSessionAccessor _sessionAccessor;

        public WriteRepository(MongoDbContext context, MongoSessionAccessor sessionAccessor)
        {
            _context = context;
            _sessionAccessor = sessionAccessor;
        }

        private IMongoCollection<TEntity> Collection => _context.GetCollection<TEntity>();

        private IClientSessionHandle? Session => _sessionAccessor.Session;

        /// <inheritdoc />
        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Session is null)
                {
                    await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
                }
                else
                {
                    await Collection.InsertOneAsync(Session, entity, cancellationToken: cancellationToken);
                }
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // 轉成與資料庫無關的例外，上層不必認識 Mongo 的型別
                throw new DuplicateKeyException($"{typeof(TEntity).Name} 資料重複", exception);
            }

            return entity;
        }

        /// <inheritdoc />
        public async Task AddManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            List<TEntity> entityList = entities.ToList();
            if (entityList.Count == 0)
            {
                return;
            }

            if (Session is null)
            {
                await Collection.InsertManyAsync(entityList, cancellationToken: cancellationToken);
            }
            else
            {
                await Collection.InsertManyAsync(Session, entityList, cancellationToken: cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            FilterDefinition<TEntity> filter = Builders<TEntity>.Filter.Eq(IdMemberName, GetId(entity));

            if (Session is null)
            {
                await Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
            }
            else
            {
                await Collection.ReplaceOneAsync(Session, filter, entity, cancellationToken: cancellationToken);
            }

            return entity;
        }

        /// <inheritdoc />
        public async Task UpdateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken =
default)
        {
            foreach (TEntity entity in entities)
            {
                await UpdateAsync(entity, cancellationToken);
            }
        }

        /// <inheritdoc />
        public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return DeleteAsync(GetId(entity), cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            await DeleteByFilterAsync(Builders<TEntity>.Filter.Eq(IdMemberName, id), cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken =
default)
        {
            List<string> ids = entities.Select(GetId).ToList();
            if (ids.Count == 0)
            {
                return;
            }

            await DeleteByFilterAsync(Builders<TEntity>.Filter.In(IdMemberName, ids), cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteManyAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            await DeleteByFilterAsync(predicate, cancellationToken);
        }

        /// <summary>依條件刪除（有 Session 時加入交易）</summary>
        private async Task DeleteByFilterAsync(FilterDefinition<TEntity> filter, CancellationToken
cancellationToken)
        {
            if (Session is null)
            {
                await Collection.DeleteManyAsync(filter, cancellationToken);
            }
            else
            {
                await Collection.DeleteManyAsync(Session, filter, cancellationToken: cancellationToken);
            }
        }

        /// <summary>從 class map 取得 Entity 的 Id 值</summary>
        private static string GetId(TEntity entity)
        {
            return (string)BsonClassMap.LookupClassMap(typeof(TEntity)).IdMemberMap.Getter(entity);
        }
    }
}