 using System.Linq.Expressions;

  namespace Game.Abstractions.Repositories
  {
      /// <summary>
      /// 唯讀 Repository；查詢走讀取節點（secondary），不在交易內
      /// </summary>
      /// <remarks>
      /// 讀取節點可能比主節點慢幾毫秒同步，剛寫入的資料可能還查不到。
      /// 交易內需要讀取時不可使用本介面。
      /// 排序、筆數限制等複雜查詢請改用專用的 QueryExecutor。
      /// </remarks>
      /// <typeparam name="TEntity">Entity 型別</typeparam>
      public interface IReadRepository<TEntity>
          where TEntity : class
      {
          /// <summary>依 Id 取得資料；找不到回傳 null</summary>
          Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

          /// <summary>依條件取得第一筆資料；找不到回傳 null</summary>
          Task<TEntity?> FirstOrDefaultAsync(
              Expression<Func<TEntity, bool>> predicate,
              CancellationToken cancellationToken = default);

          /// <summary>取得全部資料</summary>
          Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

          /// <summary>依條件取得資料清單</summary>
          Task<List<TEntity>> GetListAsync(
              Expression<Func<TEntity, bool>> predicate,
              CancellationToken cancellationToken = default);

          /// <summary>計算資料總數</summary>
          Task<int> CountAsync(CancellationToken cancellationToken = default);

          /// <summary>依條件計算資料總數</summary>
          Task<int> CountAsync(
              Expression<Func<TEntity, bool>> predicate,
              CancellationToken cancellationToken = default);

          /// <summary>檢查是否存在符合條件的資料</summary>
          Task<bool> AnyAsync(
              Expression<Func<TEntity, bool>> predicate,
              CancellationToken cancellationToken = default);
      }
  }