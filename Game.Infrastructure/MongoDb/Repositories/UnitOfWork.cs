
  using Game.Abstractions.DependencyInjection;
  using Game.Abstractions.Repositories;
  using MongoDB.Driver;

  namespace Game.Infrastructure.MongoDb
  {
      /// <summary>
      /// Unit of Work 實作：開啟 MongoDB Session 並管理交易
      /// </summary>
      /// <remarks>
      /// 開啟的 Session 會放進 <see cref="MongoSessionAccessor"/>，讓同一個 Request 的 WriteRepository 帶著它寫入。
      /// 一次只能有一個交易，不支援巢狀交易。
      /// </remarks>
      [ExposeServices(typeof(IUnitOfWork))]
      public class UnitOfWork : IUnitOfWork, IDisposable, IScopedDependency
      {
          private readonly MongoDbContext _context;
          private readonly MongoSessionAccessor _sessionAccessor;
          private IClientSessionHandle? _session;

          public UnitOfWork(MongoDbContext context, MongoSessionAccessor sessionAccessor)
          {
              _context = context;
              _sessionAccessor = sessionAccessor;
          }

          /// <inheritdoc />
          public async Task ExecuteInTransactionAsync(
              Func<CancellationToken, Task> operation,
              CancellationToken cancellationToken = default)
          {
              await StartSessionAsync(cancellationToken);

              try
              {
                  // Driver 負責 Commit / Abort，遇到暫時性錯誤會重跑整個 callback
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
                  EndSession();
              }
          }

          /// <inheritdoc />
          public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
          {
              await StartSessionAsync(cancellationToken);
              _session!.StartTransaction();
          }

          /// <inheritdoc />
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

          /// <inheritdoc />
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

          /// <summary>Request 結束時由 DI 呼叫；還沒結束的交易會隨 Session 關閉自動放棄</summary>
          public void Dispose()
          {
              EndSession();
          }

          /// <summary>開啟 Session 並放進 accessor</summary>
          private async Task StartSessionAsync(CancellationToken cancellationToken)
          {
              if (_session is not null)
              {
                  throw new InvalidOperationException("交易已經開始，不支援巢狀交易");
              }

              _session = await _context.Client.StartSessionAsync(cancellationToken: cancellationToken);
              _sessionAccessor.Session = _session;
          }

          /// <summary>清空 accessor 並關閉 Session</summary>
          private void EndSession()
          {
              _sessionAccessor.Session = null;
              _session?.Dispose();
              _session = null;
          }
      }
  }