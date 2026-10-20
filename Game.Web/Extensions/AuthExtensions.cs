// EasterEgg -> 2/9【啟動】登記三樣東西，等請求進來時才會用到：
//   ① 怎麼認人（5 假登入）
//   ② 哪支 API 需要什麼條件（Policy：名字由 4 組出，條件裡放 7）
//   ③ 誰負責檢查權限（8）
// 上一步 <- 1：Program.cs 呼叫 AddAuthServices()
// 下一步 -> 3：Game.Domain/Constants/PermissionCodes.cs（這裡用到的權限碼）
using Game.Domain.Constants;
using Game.Web.Security.Authentication;
using Game.Web.Security.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Game.Web.Extensions
{
    /// <summary>
    /// 身分驗證與授權配置擴展方法
    /// </summary>
    public static class AuthExtensions
    {
        /// <summary>
        /// 註冊驗證方案（誰在呼叫）與授權 Policy（能不能呼叫）
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <returns>服務集合（支援鏈式呼叫）</returns>
        public static IServiceCollection AddAuthServices(this IServiceCollection services)
        {
            // ① 怎麼認人：登記假登入（5）
            //    請求進來時，app.UseAuthentication() 用它決定 HttpContext.User 是誰
            //
            //    AddAuthentication("PlayerOne")：開啟驗證系統，並指定「預設用 PlayerOne 這個方案」
            //      → 每個請求都跑它來認人；要回 401/403 時也找它（所以會呼叫 5 的 HandleChallengeAsync）
            //    AddScheme<設定型別, Handler>("PlayerOne", 設定)：登記「名字 PlayerOne → 由 5 處理」
            //      → 設定型別用最基本的 AuthenticationSchemeOptions，因為我們沒有自訂設定，所以設定填 null
            //      → 內建的 AddJwtBearer() 其實就是幫你寫好的 AddScheme<JwtBearerOptions, JwtBearerHandler>("Bearer", ...)
            //    名字寫兩次是因為一個系統可以登記好幾種登入方式（JWT、Cookie…），前者選預設、後者是這個方案的名字
            //    不用 AddScoped 登記：驗證系統是用「名字」找 Handler，AddScheme 會同時登記進 DI 和名字對照表
            services.AddAuthentication(PlayerOneAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, PlayerOneAuthenticationHandler>(
                    PlayerOneAuthenticationHandler.SchemeName, configureOptions: null);

            // ② 哪支 API 需要什麼條件：登記 Policy
            //    現在是啟動時，只是把規則存起來，還沒有 User、也不做判斷
            //    請求進來時 app.UseAuthorization() 才拿出來檢查，那時 5 已經跑完
            AuthorizationBuilder builder = services.AddAuthorizationBuilder();

            // Policy 的名字是 "permission:EASTER_EGG"，6 的標籤用同一個名字找到它
            string easterEggPolicyName = PermissionPolicy.BuildName(PermissionCodes.EasterEgg);

            // 這個 Policy 有兩個條件，兩個都過才放行（寫的順序不影響結果）
            builder.AddPolicy(easterEggPolicyName, policy =>
            {
                // 條件 1：要已登入
                //   框架內建的檢查，看 User.Identity.IsAuthenticated
                //   → 5 暗號對才是 true；沒帶或打錯是 false
                policy.RequireAuthenticatedUser();

                // 條件 2：要有 EASTER_EGG 權限
                //   7 只是一張紙條，寫著「需要 EASTER_EGG」，自己不會檢查
                //   → 請求時框架把這張紙條交給 8，由 8 檢查 User 有沒有 permission = EASTER_EGG
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.EasterEgg));
            });

            // ③ 誰負責檢查權限：登記 8
            //    框架靠型別配對：8 專門處理 7 這種紙條，所以條件 2 會交給它
            //    8 只讀 User 身上的資料、不需要其他服務，整個程式共用一個就好（Singleton）
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return services;
        }
    }
}
