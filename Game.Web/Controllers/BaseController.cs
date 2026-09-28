using Game.Abstractions.Common;
using Microsoft.AspNetCore.Mvc;

namespace Game.Web.Controllers
{
    /// <summary>
    /// API 基礎控制器，提供共用回應格式
    /// </summary>
    /// <remarks>
    /// 成功：HTTP 200 + ApiResponse（Success = true）。
    /// 失敗由例外處理 Middleware 統一轉成 4xx/5xx + ApiResponse（Success = false），
    /// 不要用 Ok(ApiResponse.Error(...)) 回傳錯誤，以免前端把 2xx 誤判為成功。
    /// </remarks>
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// 成功回應（HTTP 200）
        /// </summary>
        /// <param name="data">回應資料</param>
        /// <param name="message">回應訊息</param>
        /// <returns>包成 ApiResponse 的 200 回應</returns>
        protected ActionResult<ApiResponse<T>> BusinessOkResponse<T>(T? data, string message = "操作成功") =>
            Ok(ApiResponse<T>.Ok(data, message));
    }
}