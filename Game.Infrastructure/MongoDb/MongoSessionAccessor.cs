 using Game.Abstractions.DependencyInjection;
  using MongoDB.Driver;

  namespace Game.Infrastructure.MongoDb
  {
      /// <summary>
      /// 存放目前 Request 的 MongoDB Session，讓 UnitOfWork 與 WriteRepository 共用
      /// </summary>
      /// <remarks>
      /// Scoped：同一個 Request 內拿到的是同一個實例。
      /// UnitOfWork 開啟 Session 後放進來；WriteRepository 寫入時取出使用。
      /// Session 為 null 代表目前沒有交易，寫入不帶 Session。
      /// </remarks>
      public class MongoSessionAccessor : IScopedDependency
      {
          /// <summary>目前的 Session；沒有開啟時為 null</summary>
          public IClientSessionHandle? Session { get; set; }
      }
  }