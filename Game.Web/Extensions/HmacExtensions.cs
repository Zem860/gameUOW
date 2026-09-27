using Game.Abstractions.Settings;

namespace Game.Web.Extensions
{
    /// <summary>
    /// HMAC 配置擴展方法
    /// </summary>
    public static class HmacExtensions
    {
        /// <summary>
        /// 註冊 HMAC 設定（檢查密鑰並登記 IOptions&lt;HmacSettings&gt;）
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服務集合（支援鏈式呼叫）</returns>
        /// <exception cref="InvalidOperationException">Secret 未設定時，於啟動時拋出</exception>
        public static IServiceCollection AddHmacServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 取出 appsettings中的 Hmac區塊，依屬性名稱填成HmacSettings
            IConfigurationSection hmacSection = configuration.GetSection(HmacSettings.SectionName);
            HmacSettings settings = hmacSection.Get<HmacSettings>() ?? new HmacSettings();
            // 沒有密鑰就無法簽章，啟動時直接擋下
            if (string.IsNullOrWhiteSpace(settings.Secret))
            {
                throw new InvalidOperationException($"HMAC Secret is not configured. Please set '{HmacSettings.SectionName}:Secret' in appsettings or environment variables.");
            }
            // 讓 HmacService 能用 IOptions<HmacSettings> 取得設定
            services.Configure<HmacSettings>(hmacSection);
            return services;
        }
    }
}