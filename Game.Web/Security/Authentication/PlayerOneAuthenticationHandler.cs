// EasterEgg -> 5/9【請求】你是誰？讀 ?playerone=zem，對了就建立 User 放進 HttpContext.User
// 上一步 <- 1：Program.cs 的 app.UseAuthentication() 每個請求都會呼叫這裡
// 下一步 -> 6：Security/Authorization/HasPermissionAttribute.cs
using System.Security.Claims;
using System.Text.Encodings.Web;
using Game.Abstractions.Common;
using Game.Domain.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Game.Web.Security.Authentication
{
    /// <summary>
    /// 彩蛋用的假登入：網址帶 ?playerone=zem 就視為玩家 zem 已登入，並給他彩蛋權限
    /// </summary>
    /// <remarks>
    /// 網址參數沒有簽章，任何人知道暗號都能自己打，所以只能用在彩蛋這種不怕被看到的功能。
    /// 需要真正身分驗證的 API 必須改用有簽章的 Token。
    /// </remarks>
    public sealed class PlayerOneAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>驗證方案名稱（AddScheme 與 DefaultScheme 使用）</summary>
        public const string SchemeName = "PlayerOne";

        private const string QueryKey = "playerone";
        private const string SecretPlayerName = "zem";

        /// <summary>
        /// 建立假登入驗證處理器
        /// </summary>
        /// <param name="options">驗證方案選項</param>
        /// <param name="logger">記錄器工廠</param>
        /// <param name="encoder">URL 編碼器</param>
        public PlayerOneAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// <summary>
        /// 每個請求進來時執行：暗號正確就建立帶有彩蛋權限的使用者，否則維持匿名
        /// </summary>
        /// <returns>
        /// 暗號正確回傳 Success（UseAuthentication 會把使用者放進 HttpContext.User）；
        /// 沒帶或打錯回傳 NoResult（使用者維持匿名，交給授權決定要不要擋）
        /// </returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // ① 從網址拿暗號：?playerone=zem → "zem"（使用者只送了這個，請求裡沒有任何權限資訊）
            string? playerName = Request.Query[QueryKey];

            // ② 比對暗號：不是 zem 就不建立 User，HttpContext.User 維持匿名（IsAuthenticated = false）
            if (!string.Equals(playerName, SecretPlayerName, StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // ③ 暗號對了，由伺服器決定發給他哪些 claim（權限是這裡寫死給的，不是使用者送來的）：
            //    name = zem、permission = EASTER_EGG；8 之後只看這張證件，不知道 zem 是什麼
            Claim[] claims =
            [
                new Claim(ClaimTypes.Name, SecretPlayerName),
                new Claim(PermissionCodes.ClaimType, PermissionCodes.EasterEgg),
            ];

            // ④ 把 claims 包成 User：第二個參數是驗證方案名稱，有填 IsAuthenticated 才會是 true
            ClaimsIdentity identity = new(claims, SchemeName);
            ClaimsPrincipal principal = new(identity);
            AuthenticationTicket ticket = new(principal, SchemeName);

            // ⑤ 回傳 Success：app.UseAuthentication() 收到後把 principal 放進 HttpContext.User
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        /// <summary>
        /// 未登入卻存取需要授權的 API 時執行：回 401 + ApiResponse，跟其他錯誤格式一致
        /// </summary>
        /// <param name="properties">驗證屬性（未使用）</param>
        /// <returns>寫完回應的 Task</returns>
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(
                ApiResponse<object>.Error("尚未登入", "UNAUTHORIZED"), Context.RequestAborted);
        }

        /// <summary>
        /// 已登入但權限不足時執行：回 403 + ApiResponse，跟其他錯誤格式一致
        /// </summary>
        /// <param name="properties">驗證屬性（未使用）</param>
        /// <returns>寫完回應的 Task</returns>
        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            await Response.WriteAsJsonAsync(
                ApiResponse<object>.Error("沒有權限", "FORBIDDEN"), Context.RequestAborted);
        }
    }
}
