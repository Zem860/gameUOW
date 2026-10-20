// EasterEgg -> 4/9【啟動】Policy 命名規則：Policy 名稱 = "permission:" + 權限碼（2 註冊時、6 標籤裡都用 BuildName() 算出同一個名字）
// 上一步 <- 3：Game.Domain/Constants/PermissionCodes.cs
// 下一步 -> 5：Security/Authentication/PlayerOneAuthenticationHandler.cs（從這裡開始是「請求進來時」）
namespace Game.Web.Security.Authorization
{
    /// <summary>
    /// 權限 Policy 命名規則（以 <see cref="Game.Domain.Constants.PermissionCodes"/> 為準）
    /// </summary>
    public static class PermissionPolicy
    {
        /// <summary>權限 Policy 名稱前綴（跟其他種類的 Policy 區隔，避免撞名）</summary>
        public const string Prefix = "permission:";

        /// <summary>
        /// 由權限代碼組出 Policy 名稱（註冊 Policy 與 <see cref="HasPermissionAttribute"/> 共用，確保兩邊名字一致）
        /// </summary>
        /// <param name="permissionCode">權限代碼</param>
        /// <returns>Policy 名稱，例如 permission:EASTER_EGG</returns>
        public static string BuildName(string permissionCode) => Prefix + permissionCode;
    }
}
