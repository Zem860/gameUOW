
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
        IGameRepository Games { get; }

        /// <summary>遊戲結果 Repository</summary>
        IGameResultRepository GameResults { get; }

        /// <summary>開始交易</summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        /// <summary>提交交易</summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>回滾交易</summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 在同一個 MongoDB 交易中執行作業。
        /// <para>作業成功 → 自動 Commit；作業拋出例外 → 自動 Rollback（Abort）。</para>
        /// <para>遇到暫時性錯誤（TransientTransactionError / UnknownTransactionCommitResult）會自動重試；
        /// 實作以 MongoDB Driver 的 <c>WithTransactionAsync</c> 包裝。</para>
        /// </summary>
        /// <remarks>
        /// Application Service 一律使用此方法；<see cref="BeginTransactionAsync"/> / <see cref="CommitAsync"/> / <see cref="RollbackAsync"/> 保留給特殊情況。
        /// <para>注意：作業可能因重試被執行多次，不可在其中呼叫 Redis 等不受 MongoDB 交易控制的外部服務，
        /// 那些要放在本方法完成之後。</para>
        /// <para>作業內呼叫 Repository 當下就會寫入（在交易中，Commit 前其他人看不到），不需要另外存檔。</para>
        /// </remarks>
        /// <param name="operation">要在交易中執行的作業；參數為取消權杖。</param>
        /// <param name="cancellationToken">取消權杖。</param>
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
    }

}