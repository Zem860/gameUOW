using System.Threading.RateLimiting;
using Game.Abstractions.Common;
using Game.Abstractions.Settings;
using Game.Domain.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace Game.Web.Extensions
{
    /// <summary>
    /// 請求限流配置擴展方法
    /// </summary>
    public static class RateLimitExtensions
    {
        // 取不到 IP 時的共用分組（極少發生，例如測試環境）
        private const string UnknownClientIp = "unknown";

        /// <summary>
        /// 註冊請求限流：每個 policy 依 IP 分組、固定視窗計數，超過回 429 + ApiResponse
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服務集合（支援鏈式呼叫）</returns>
        /// <exception cref="InvalidOperationException">次數或秒數不是正數時，於啟動時拋出</exception>
        public static IServiceCollection AddRateLimitingServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ① json 的 "RateLimiting" 區塊 → 依屬性名稱填成 RateLimitSettings
            RateLimitSettings settings = configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();
            ValidateLimit(nameof(settings.GameStart), settings.GameStart);
            ValidateLimit(nameof(settings.GameResult), settings.GameResult);

            services.AddRateLimiter(options =>
            {
                // ② policy 名稱（常數）配上它的數字（設定）
                AddPerIpFixedWindowPolicy(options, RateLimitPolicyNames.GameStart, settings.GameStart);
                AddPerIpFixedWindowPolicy(options, RateLimitPolicyNames.GameResult, settings.GameResult);
                options.OnRejected = async(context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsJsonAsync(
                        ApiResponse<object>.Error("請求過於頻繁，請稍後再試", "RATE_LIMIT_EXCEEDED"),cancellationToken);
                };
            });

            return services;
        }

        /// <summary>
        /// 加入一個依 IP 分組的固定視窗 policy：每個 IP 各自一個計數器
        /// </summary>
        /// <param name="options">限流選項</param>
        /// <param name="policyName">policy 名稱（[EnableRateLimiting] 用這個名稱找到它）</param>
        /// <param name="limit">次數與視窗秒數</param>
        private static void AddPerIpFixedWindowPolicy(RateLimiterOptions options, string policyName, FixedWindowLimitSettings limit)
        {
            options.AddPolicy(policyName, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit.PermitLimit,
                Window = TimeSpan.FromSeconds(limit.WindowSeconds),
                QueueLimit = 0,
            }
            ));
        }

        /// <summary>
        /// 檢查次數與秒數皆為正數；設定打錯被綁成 0 時，啟動就擋下
        /// </summary>
        /// <param name="name">設定名稱（顯示在錯誤訊息）</param>
        /// <param name="limit">要檢查的設定</param>
        /// <exception cref="InvalidOperationException">次數或秒數不是正數</exception>
        private static void ValidateLimit(string name, FixedWindowLimitSettings limit)
        {
            if (limit.PermitLimit <= 0 || limit.WindowSeconds <= 0)
            {
                throw new InvalidOperationException($"'{RateLimitSettings.SectionName}:{name}' PermitLimit and WindowSeconds must be positive.");
            }
        }
    }
}