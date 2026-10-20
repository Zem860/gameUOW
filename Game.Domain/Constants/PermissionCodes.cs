// EasterEgg -> 3/9【啟動】權限碼常數：5 塞進 claim、4 組成 Policy 名稱、6 貼在 API 上、8 拿來比對，全部共用這裡
// 上一步 <- 2：Extensions/AuthExtensions.cs
// 下一步 -> 4：Game.Web/Security/Authorization/PermissionPolicy.cs
namespace Game.Domain.Constants
{
    /// <summary>
    /// 權限代碼（放在使用者 Claims 裡，授權 Handler 依此判斷能否存取）
    /// </summary>
    public static class PermissionCodes
    {
        /// <summary>存放權限代碼的 Claim 類型名稱</summary>
        public const string ClaimType = "permission";

        /// <summary>可查看彩蛋</summary>
        public const string EasterEgg = "EASTER_EGG";
    }
}
