// EasterEgg -> 9/9【請求】終點：前面全部通過才會執行 Get()，回傳彩蛋
// 上一步 <- 8：PermissionAuthorizationHandler.cs 呼叫了 Succeed
// （回頭看：方法上的 [HasPermission] 就是 6）
using Game.Abstractions.Common;
using Game.Domain.Constants;
using Game.Web.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game.Web.Controllers
{
    /// <summary>
    /// 彩蛋 API（需要彩蛋權限）
    /// </summary>
    [Route("api/easter-egg")]
    public sealed class EasterEggController : BaseController
    {
        /// <summary>
        /// 取得彩蛋訊息
        /// </summary>
        /// <returns>給已登入玩家的彩蛋訊息</returns>
        [HttpGet]
        [HasPermission(PermissionCodes.EasterEgg)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public ActionResult<ApiResponse<string>> Get()
        {
            // 能進到這裡代表授權已通過，User.Identity.Name 一定有值
            string playerName = User.Identity!.Name!;
            return BusinessOkResponse($"🥚 Player One {playerName}，你找到彩蛋了！", "彩蛋");
        }
    }
}
