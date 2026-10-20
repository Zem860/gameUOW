// EasterEgg -> 6/9【請求】API 上的標籤：告訴 app.UseAuthorization() 這支 API 要套哪個 Policy（名稱用 4 的 BuildName() 算）
// 上一步 <- 5：PlayerOneAuthenticationHandler.cs（User 已經放好）
// 下一步 -> 7：PermissionRequirement.cs（Policy 裡裝的東西）
using Microsoft.AspNetCore.Authorization;

namespace Game.Web.Security.Authorization
{
    /// <summary>
    /// 要求使用者擁有指定權限才能呼叫（對應 <see cref="PermissionPolicy.BuildName"/> 註冊的 Policy）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class HasPermissionAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// 建立權限授權標籤
        /// </summary>
        /// <param name="permissionCode"><see cref="Game.Domain.Constants.PermissionCodes"/> 裡的常數</param>
        public HasPermissionAttribute(string permissionCode)
        {
            Policy = PermissionPolicy.BuildName(permissionCode);
        }
    }
}
