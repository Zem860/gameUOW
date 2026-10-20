// EasterEgg -> 8/9【請求】你能做嗎？看 5 放進去的 User 有沒有 7 要求的權限 claim
// 上一步 <- 7：PermissionRequirement.cs
// 下一步 -> 9：Controllers/EasterEggController.cs（Succeed 才進得去；失敗回到 5 的 HandleChallengeAsync 回 401）
using Game.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Game.Web.Security.Authorization
{
    /// <summary>
    /// 權限授權處理器：檢查使用者的 Claims 裡有沒有需求指定的權限代碼
    /// </summary>
    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        /// <summary>
        /// 有對應權限就標記成功；沒有就什麼都不做，框架會視為授權失敗
        /// </summary>
        /// <param name="context">授權內容；context.User 是 UseAuthentication 先放進去的使用者</param>
        /// <param name="requirement">要檢查的權限需求</param>
        /// <returns>完成的 Task（只讀 Claims，不查資料庫）</returns>
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return Task.CompletedTask;
            }

            if (context.User.HasClaim(PermissionCodes.ClaimType, requirement.PermissionCode))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
