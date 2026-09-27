using Game.Abstractions.DependencyInjection;
using Game.Abstractions.Repositories;
using MongoDB.Driver;

namespace Game.Infrastructure.MongoDb
{
    /// <summary>
    /// Unit of Work 實作：開啟 MongoDB Session 並管理交易，讓一組寫入「全部成功，或全部不算」
    /// </summary>
    /// <remarks>
    /// <para>
    /// 使用位置：Application Service 注入 <see cref="IUnitOfWork"/>，把要在交易內做的寫入包成 lambda 交給
    /// <see cref="ExecuteInTransactionAsync"/>；Service 不直接碰 Session。
    /// </para>
    /// <para>
    /// 運作：開啟的 Session 放進 <see cref="MongoSessionAccessor"/>（同一個 Request 共用），
    /// WriteRepository 寫入時從 accessor 取出 Session，寫入就會加入同一個交易；結束後清空 accessor 並關閉 Session。
    /// </para>
    /// <para>
    /// 限制：同一個 Request 一次只能有一個交易（不支援巢狀）；交易內不可平行寫入（Session 不是執行緒安全）；
    /// ReadRepository 不看 accessor，讀取不在交易內。
    /// </para>
    /// <para>
    /// 註冊：Scoped（每個 Request 一個）；只用 IUnitOfWork 登記，IDisposable 不登記。
    /// </para>
    /// </remarks>
    [ExposeServices(typeof(IUnitOfWork))]
    public class UnitOfWork : IUnitOfWork, IDisposable, IScopedDependency
    {
        private readonly MongoDbContext _context;
        private readonly MongoSessionAccessor _sessionAccessor;

        /// <summary>目前開啟的 Session；沒有交易時為 null</summary>
        private IClientSessionHandle? _session;

        public UnitOfWork(MongoDbContext context, MongoSessionAccessor sessionAccessor)
        {
            _context = context;
            _sessionAccessor = sessionAccessor;
        }

        /// <summary>
        /// 自動版交易（Service 一律用這個）：開交易 → 執行作業 → 成功 Commit、例外 Abort、暫時性錯誤重跑整段
        /// </summary>
        /// <param name="operation">要在交易內執行的作業</param>
        /// <param name="cancellationToken">取消權杖</param>
        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await StartSessionAsync(cancellationToken);

            try
            {
                // Driver 負責 Commit / Abort，遇到暫時性錯誤會重跑整個 callback；
                // callback 規定要回傳值，這裡用不到，回傳 true 即可
                await _session!.WithTransactionAsync(
                    async (_, token) =>
                    {
                        await operation(token);
                        return true;
                    },
                    cancellationToken: cancellationToken);
            }
            finally
            {
                // 不論成功或失敗都清空 accessor、關閉 Session
                EndSession();
            }
        }

        /// <summary>
        /// 手動版第 1 步：開始交易（之後一定要呼叫 <see cref="CommitAsync"/> 或 <see cref="RollbackAsync"/>）
        /// </summary>
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            await StartSessionAsync(cancellationToken);
            _session!.StartTransaction();
        }

        /// <summary>
        /// 手動版第 2 步（成功）：提交交易，交易內的寫入全部生效
        /// </summary>
        /// <exception cref="InvalidOperationException">尚未開始交易</exception>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            IClientSessionHandle session = _session ?? throw new InvalidOperationException("尚未開始交易");

            try
            {
                await session.CommitTransactionAsync(cancellationToken);
            }
            finally
            {
                EndSession();
            }
        }

        /// <summary>
        /// 手動版第 2 步（失敗）：放棄交易，交易內的寫入全部不算
        /// </summary>
        /// <exception cref="InvalidOperationException">尚未開始交易</exception>
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            IClientSessionHandle session = _session ?? throw new InvalidOperationException("尚未開始交易");

            try
            {
                await session.AbortTransactionAsync(cancellationToken);
            }
            finally
            {
                EndSession();
            }
        }

        /// <summary>
        /// Request 結束時由 DI 自動呼叫；忘記 Commit 的交易會隨 Session 關閉自動放棄
        /// </summary>
        public void Dispose()
        {
            EndSession();
        }

        /// <summary>
        /// 開啟 Session 並放進 accessor；已有交易時拋出例外（避免第二個 Session 覆蓋 accessor）
        /// </summary>
        private async Task StartSessionAsync(CancellationToken cancellationToken)
        {
            if (_session is not null)
            {
                throw new InvalidOperationException("交易已經開始，不支援巢狀交易");
            }

            _session = await _context.Client.StartSessionAsync(cancellationToken: cancellationToken);
            _sessionAccessor.Session = _session;
        }

        /// <summary>
        /// 清空 accessor 並關閉 Session（之後的寫入不再帶 Session）
        /// </summary>
        private void EndSession()
        {
            _sessionAccessor.Session = null;
            _session?.Dispose();
            _session = null;
        }
    }
}
