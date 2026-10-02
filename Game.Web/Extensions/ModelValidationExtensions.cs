using Game.Abstractions.Common;
using Microsoft.AspNetCore.Mvc;

namespace Game.Web.Extensions
{
    /// <summary>
    /// Model 驗證失敗回應的設定
    /// </summary>
    /// <remarks>
    /// [ApiController] 會在呼叫 Action 之前，由框架依 Request DTO 上的
    /// [Required]、[StringLength] 等規則驗證；沒過就不呼叫 Action，直接回 400。
    /// 這個 400 是「回傳結果」而不是「丟例外」，GlobalExceptionHandlingMiddleware 接不到，
    /// 所以要在這裡另外把格式統一成 ApiResponse。
    /// </remarks>
    public static class ModelValidationExtensions
    {
        /// <summary>
        /// 將 [ApiController] 自動驗證失敗的回應，從內建 ProblemDetails 改成 ApiResponse
        /// </summary>
        /// <param name="builder">AddControllers() 回傳的 MVC 建構器</param>
        /// <returns>同一個建構器，讓呼叫端可以繼續串接其他設定</returns>
        public static IMvcBuilder AddApiResponseValidation(this IMvcBuilder builder)
        {
            // ConfigureApiBehaviorOptions = services.Configure<ApiBehaviorOptions>(...)，
            // 對全部有 [ApiController] 的 Controller 生效
            builder.ConfigureApiBehaviorOptions(options =>
            {
                // 這裡只是把 lambda 存起來，不會馬上執行；
                // 等驗證失敗時，框架才呼叫它產生回應（只改格式，不改驗證規則）
                options.InvalidModelStateResponseFactory = context =>
                {
                    // ModelState.Values：每個欄位一筆，底下可能有多個錯誤 → 攤平後串成一句訊息
                    string message = string.Join(";", context.ModelState.Values
                        .SelectMany(entry => entry.Errors)
                        .Select(error => error.ErrorMessage));

                    // ErrorCode 與 Middleware 處理 ArgumentException 時相同，前端只需判斷一種代碼
                    return new BadRequestObjectResult(ApiResponse<object>.Error(message, "VALIDATION_ERROR"));
                };
            });

            return builder;
        }
    }
}
