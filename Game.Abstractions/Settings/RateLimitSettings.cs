namespace Game.Abstractions.Settings
{
    /// <summary>
    /// 請求限流設定（每個 IP 各自計數）
    /// </summary>
    public class RateLimitSettings
    {
        /// <summary>
        /// 設定節點名稱
        /// </summary>
        public string SectionName { get; set; } = string.Empty;

        /// <summary>
        /// 開始遊戲（POST /api/games/{code}/start）
        /// </summary>
        public FixWindowLimitSettings GameStart { get; set; } = new FixWindowLimitSettings();
        /// <summary>
        /// 儲存遊戲結果（POST /api/games/{code}/result）
        /// </summary>
        public FixWindowLimitSettings GameResult { get; set; } = new FixWindowLimitSettings();

    }
}