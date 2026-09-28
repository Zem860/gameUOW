using System.Net;
using Game.Abstractions.Common;
using Game.Abstractions.Exceptions;

namespace Game.Web.Middlewares
{
    /// <summary>
    /// 全域例外處理中介軟體：攔截未處理的例外，統一轉成 ApiResponse 與對應的 HTTP 狀態碼
    /// </summary>
    /// <remarks>
    /// Service 只負責丟例外，不處理 HTTP；狀態碼的對應集中在 <see cref="ClassifyException"/>。
    /// </remarks>
    public class GlobalExceptionHandlingMiddleware
    {
        // HttpStatusCode 沒有對應值；採 nginx 慣例 499 表示客戶端中斷
        private const int ClientClosedRequestStatusCode = 499;

        /// <summary>
        /// 下一個中介軟體；呼叫它請求才會繼續往下走（最後到 Controller），
        /// 下游做完或拋出例外都會回到呼叫它的地方
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// 記錄器；輸出位置由 Logging 設定決定（開發時印在執行 dotnet run 的主控台）
        /// </summary>
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        /// <summary>
        /// 建立全域例外處理中介軟體
        /// </summary>
        /// <param name="next">下一個中介軟體</param>
        /// <param name="logger">記錄器</param>
        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// 執行後續流程並攔截例外
        /// </summary>
        /// <param name="context">目前的 HTTP 內容</param>
        /// <returns>非同步工作</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // 客戶端自己斷線，不是系統錯誤；連線已斷，只設定狀態碼
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = ClientClosedRequestStatusCode;
                }
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            (HttpStatusCode statusCode, string message, string errorCode) = ClassifyException(exception);
            LogException(exception, statusCode);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("回應已經開始送出，無法改寫為錯誤回應");
                return;
            }

            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Error(message, errorCode));
        }

        /// <summary>
        /// 依例外型別決定狀態碼、對外訊息與錯誤代碼
        /// </summary>
        private static (HttpStatusCode StatusCode, string Message, string ErrorCode) ClassifyException(Exception exception)
        {
            return exception switch
            {
                KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message, "NOT_FOUND"),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message, "UNAUTHORIZED"),
                DuplicateKeyException => (HttpStatusCode.Conflict, "資料重複，可能已經送出過", "DUPLICATE"),
                ArgumentException => (HttpStatusCode.BadRequest, exception.Message, "VALIDATION_ERROR"),

                // 未預期的錯誤：細節只寫進 log，不回傳給前端，避免洩漏內部資訊
                _ => (HttpStatusCode.InternalServerError, "伺服器發生錯誤", "INTERNAL_SERVER_ERROR"),
            };
        }

        /// <summary>
        /// 5xx 記為 Error；4xx 是可預期的使用者錯誤，記為 Information 避免干擾
        /// </summary>
        private void LogException(Exception exception, HttpStatusCode statusCode)
        {
            if ((int)statusCode >= 500)
            {
                _logger.LogError(exception, "發生未處理的例外：{Message}", exception.Message);
                return;
            }

            _logger.LogInformation("請求失敗（{StatusCode}）：{Message}", (int)statusCode, exception.Message);
        }
    }
}
