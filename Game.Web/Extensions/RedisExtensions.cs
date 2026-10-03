using Game.Abstractions.Settings;
using StackExchange.Redis;

namespace Game.Web.Extensions
{
    /// <summary>
    /// Redis 配置擴展方法
    /// </summary>
    public static class RedisExtensions
    {
        /// <summary>
        /// 註冊 Redis 服務（設定、連線多工器）
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服務集合（支援鏈式呼叫）</returns>
        /// <exception cref="InvalidOperationException">ConnectionString 未設定時，於啟動時拋出</exception>
        public static IServiceCollection AddRedisServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 設定在記憶體中是攤平的字串字典（例如 "Redis:ConnectionString" → "localhost:6379"）；
            // GetSection 只是指向 "Redis:" 開頭的那一區，拿到的仍是字串，還不是物件
            IConfigurationSection redisSection = configuration.GetSection(RedisSettings.SectionName);

            // Get<T>()：從這一區「讀出」設定，new 一個 RedisSettings 並依屬性名稱填值後回傳；
            // 只有 appsettings 完全沒有 Redis 區塊時才回 null，此時改用空物件，讓下方檢查丟出清楚的錯誤訊息
            RedisSettings settings = redisSection.Get<RedisSettings>() ?? new RedisSettings();
            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException($"{RedisSettings.SectionName}:ConnectionString 未設定");
            }
            services.Configure<RedisSettings>(redisSection);

            // StackExchange.Redis 套件裡「Redis 連線」的介面
            // ConnectionMultiplexer 內含連線管理且執行緒安全，整個應用程式共用一個
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                ConfigurationOptions options = ConfigurationOptions.Parse(settings.ConnectionString);

                // Redis 連不上時不丟例外，背景持續重連；讓程式照常啟動，排行榜改走 fallback
                options.AbortOnConnectFail = false;

                return ConnectionMultiplexer.Connect(options);
            });
            return services;
        }
    }
}