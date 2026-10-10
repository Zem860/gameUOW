namespace Game.Abstractions.Settings
{
    /// <summary>
    /// 跨來源請求（CORS）設定
    /// </summary>
    public class CorsSettings
    {
        /// <summary>
        /// 設定節點名稱
        /// </summary>
        public const string SectionName = "Cors";
        /// <summary>
        /// 允許呼叫 API 的前端來源（scheme + host + port，例如 http://localhost:5173，結尾不加斜線）；
        /// 部署用環境變數 Cors__AllowedOrigins__0、Cors__AllowedOrigins__1…
        /// </summary>
        public string[] AllowedOrigins { get; set; } = [];
    }
}