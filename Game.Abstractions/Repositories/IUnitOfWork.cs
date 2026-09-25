
namespace Game.Abstractions.Repositories
{
    /// <summary>
    /// Unit of Work：統一提供 Repository，並管理 MongoDB Session / Transaction
    /// </summary>
    /// <remarks>
    /// 與 EF Core 不同：MongoDB 沒有 Change Tracker / SaveChanges，
    /// Repository 的 InsertAsync 呼叫當下就會寫入資料庫。
    /// 本介面的責任是讓同一請求內的 Repository 共用同一個 Session，並控制交易的開始、提交與回滾。
    /// </remarks>
    public interface IUnitOfWork
    {
        /// <summary>遊戲 Repository</summary>
        IGameResultRepository Games { get; }

        /// <summary>遊戲結果 Repository</summary>
        IGameResultRepository GameResults { get; }

        /// <summary>開始交易</summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        /// <summary>回滾交易</summary> 
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }

}