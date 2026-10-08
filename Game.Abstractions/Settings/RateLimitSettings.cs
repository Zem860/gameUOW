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
        public const string SectionName = "RateLimiting";

        /// <summary>
        /// 開始遊戲（POST /api/games/{code}/start）
        /// </summary>
        public FixedWindowLimitSettings GameStart { get; set; } = new (){PermitLimit = 30};
          /// <summary>
          /// 儲存遊戲結果（POST /api/game-results）
          /// </summary>
        public FixedWindowLimitSettings GameResult { get; set; } = new FixedWindowLimitSettings() {PermitLimit = 10};

    }
}