// EasterEgg -> 7/9【請求】Policy 裡的需求：只裝資料「需要 EASTER_EGG 權限」，不做判斷
// 上一步 <- 6：HasPermissionAttribute.cs 指定了 Policy（Policy 是在 2 建立的）
// 下一步 -> 8：PermissionAuthorizationHandler.cs（拿這個需求去判斷）
using Microsoft.AspNetCore.Authorization;

namespace Game.Web.Security.Authorization
{
    /// <summary>
    /// 授權需求：使用者必須擁有指定的權限代碼（一個 Policy 放一個）
    /// </summary>
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>需要的權限代碼（見 <see cref="Game.Domain.Constants.PermissionCodes"/>）</summary>
        public string PermissionCode { get; }

        /// <summary>
        /// 建立權限需求
        /// </summary>
        /// <param name="permissionCode">需要的權限代碼</param>
        /// <exception cref="ArgumentException">權限代碼為空白時拋出</exception>
        public PermissionRequirement(string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode))
            {
                throw new ArgumentException("權限代碼不能為空", nameof(permissionCode));
            }

            PermissionCode = permissionCode;
        }
    }
}
