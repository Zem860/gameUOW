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

        /// <summary>
        /// 建立唯讀 Repository
        /// </summary>
        /// <param name="context">Mongo 連線內容（依 Entity 型別取得 collection）</param>
        public ReadRepository(MongoDbContext context)
        {
            _context = context;
        }

        /// <summary>指定讀取節點的 collection</summary>
        private IMongoCollection<TEntity> Collection =>
            _context.GetCollection<TEntity>().WithReadPreference(ReadPreference.SecondaryPreferred);

        /// <summary>
        /// 依 Id 取得資料
        /// </summary>
        /// <param name="id">資料 Id</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>找到的資料；找不到或 Id 格式不符時回傳 null</returns>
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

        /// <summary>
        /// 依條件取得第一筆資料
        /// </summary>
        /// <param name="predicate">查詢條件（由 Driver 翻譯成 Mongo filter，在資料庫端過濾）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>第一筆符合條件的資料；沒有符合時回傳 null</returns>
        public async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await Collection.Find(predicate).FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// 取得全部資料（不加條件）
        /// </summary>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>全部資料；collection 為空時回傳空清單</returns>
        public async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await Collection.Find(FilterDefinition<TEntity>.Empty).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 依條件取得資料清單
        /// </summary>
        /// <param name="predicate">查詢條件（由 Driver 翻譯成 Mongo filter，在資料庫端過濾）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>符合條件的資料；沒有符合時回傳空清單</returns>
        public async Task<List<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await Collection.Find(predicate).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 計算資料總數
        /// </summary>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>資料筆數</returns>
        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return (int)await Collection.CountDocumentsAsync(FilterDefinition<TEntity>.Empty, cancellationToken:
cancellationToken);
        }

        /// <summary>
        /// 依條件計算資料總數
        /// </summary>
        /// <param name="predicate">查詢條件</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>符合條件的資料筆數</returns>
        public async Task<int> CountAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return (int)await Collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 檢查是否存在符合條件的資料（找到一筆就停止）
        /// </summary>
        /// <param name="predicate">查詢條件</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>存在時回傳 true</returns>
        public async Task<bool> AnyAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await Collection.Find(predicate).Limit(1).AnyAsync(cancellationToken);
        }
    }
}