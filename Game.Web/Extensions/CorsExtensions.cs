using Game.Abstractions.Settings;

namespace Game.Web.Extensions
{
    /// <summary>
    /// 跨來源請求（CORS）配置擴展方法
    /// </summary>
    public static class CorsExtensions
    {
        /// <summary>
        /// 註冊 CORS 預設 policy：只允許設定中的前端來源，以 GET / POST 搭配 JSON 呼叫 API
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服務集合（支援鏈式呼叫）</returns>
        /// <exception cref="InvalidOperationException">AllowedOrigins 未設定時，於啟動時拋出</exception>
        public static IServiceCollection AddCorsServices(this IServiceCollection services, IConfiguration configuration)
        {
            CorsSettings settings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
            if (settings.AllowedOrigins.Length == 0)
            {
                throw new InvalidOperationException($"{CorsSettings.SectionName}:AllowedOrigins未設定");
            }

            services.AddCors(options => options.
            AddDefaultPolicy(policy => policy.WithOrigins(settings.AllowedOrigins)
            .WithMethods("GET", "POST").WithHeaders("Content-Type")));
            
            return services;
        }
    }
}