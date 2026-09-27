namespace Game.Abstractions.Settings
{
    /// <summary>
    /// HMAC 簽章設定（用來簽發與驗證遊戲票券）
    /// </summary>
    public class HmacSettings
    {
        /// <summary>
        /// 設定節點名稱
        /// </summary>
        public const string SectionName = "Hmac";
        /// <summary>
        /// 簽章密鑰（機密：本機放 appsettings.Development.local.json，部署用環境變數Hmac__Secret）
        /// </summary>
        public string Secret {set; get;} = string.Empty;
    }
}