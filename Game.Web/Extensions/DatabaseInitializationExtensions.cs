using Game.Abstractions.Initialization;

namespace Game.Web.Extensions
{
    /// <summary>
    /// 資料庫初始化擴展方法
    /// </summary>
    public static class DatabaseInitializationExtensions
    {
        /// <summary>
        /// 啟動時建立索引與種子資料；失敗會拋出例外，程式不會開始接收 Request
        /// </summary>
        /// <param name="host">應用程式主機（<c>builder.Build()</c> 的結果）</param>
        /// <param name="cancellationToken">取消權杖</param>
        public static async Task InitializeDatabaseAsync(this IHost host, CancellationToken cancellationToken = default)
        {
            // 啟動時沒有 Request，自己開一個 Scope，才能取得 Scoped 的服務
            using IServiceScope scope = host.Services.CreateScope();

            IDatabaseInitializer databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            await databaseInitializer.InitializeAsync(cancellationToken);
        }
    }
}
