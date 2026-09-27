using System.Linq.Expressions;
namespace Game.Abstractions.Repositories
{
    /// <summary>
    /// 寫入 Repository；寫入主節點（primary），不含查詢方法
    /// </summary>
    /// <remarks>
    /// 呼叫當下就會寫入資料庫，沒有另外的存檔步驟。
    /// 在 <see cref="IUnitOfWork.ExecuteInTransactionAsync"/> 內呼叫時，會自動加入同一個交易。
    /// </remarks>
    /// <typeparam name="TEntity">Entity 型別</typeparam>
    public interface IWriteRepository<TEntity> where TEntity : class
    {
        /// <summary>新增資料；違反唯一限制（例：Id 重複）時拋出例外</summary>
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>批次新增資料</summary>
        Task AddManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        /// <summary>更新資料（以 Id 找到整筆取代）</summary>
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        /// <summary>批次更新資料</summary>    

        Task UpdateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        /// <summary>刪除資料</summary>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>批次刪除資料</summary>
        Task DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>依條件批次刪除資料</summary>
        Task DeleteManyAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);
    }
}