using Game.Abstractions.Settings;
using MongoDB.Driver;

namespace Game.Web.Extensions
{
    /// <summary>
    /// MongoDB 配置擴展方法
    /// </summary>
    public static class MongoDbExtensions
    {
        /// <summary>
        /// 註冊 MongoDB 服務（設定、Client、Database）
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服務集合（支援鏈式呼叫）</returns>
        /// <exception cref="InvalidOperationException">ConnectionString 或 DatabaseName 未設定時，於啟動時拋出</exception>
        public static IServiceCollection AddMongoDbServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 取出appsettings中的 MongoDb 區塊，依屬性名稱填成 MongoDbSettings
            IConfigurationSection mongoDbSection = configuration.GetSection(MongoDbSettings.SectionName);
            MongoDbSettings settings = mongoDbSection.Get<MongoDbSettings>() ?? new MongoDbSettings();

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException($"{MongoDbSettings.SectionName}:ConnectionString 未設定");
            }

            if (string.IsNullOrWhiteSpace(settings.DatabaseName))
            {
                throw new InvalidOperationException($"{MongoDbSettings.SectionName}:DatabaseName 未設定");
            }

            // 以下只登記建立方式，實際建立發生在第一次被注入時

            // 讓其他類別也能用 IOptions<MongoDbSettings> 取得設定
            services.Configure<MongoDbSettings>(mongoDbSection);

            // MongoClient 內含連線池且執行緒安全，整個應用程式共用一個
            services.AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString));
            services.AddSingleton<IMongoDatabase>(sp =>
                sp.GetRequiredService<IMongoClient>().GetDatabase(settings.DatabaseName));

            return services;
        }
    }
}
