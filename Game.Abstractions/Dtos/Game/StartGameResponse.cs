namespace Game.Abstractions.Dtos.Game
{
    /// <summary>
    /// 開始遊戲回應（遊戲票券）；前端原封不動保存，存檔時連同名字與分數送回
    /// </summary>
    public sealed class StartGameResponse
    {
        /// <summary>
        /// 預先產生的遊戲結果識別碼（GUID）；存檔時成為 GameResult 的 _id，重複送出會撞唯一鍵
        /// </summary>
        public string GameResultId { get; set; } = string.Empty;

        /// <summary>
        /// 遊戲識別碼（ObjectId 字串）
        /// </summary>
        public string GameId { get; set; } = string.Empty;

        /// <summary>
        /// 開始時間（Unix 秒）；用整數避免日期字串格式不同導致簽章不一致
        /// </summary>
        public long StartedAt { get; set; }
        /// <summary>
        /// 一次性隨機值
        /// </summary>
        public string Nonce { get; set; } = string.Empty;

        /// <summary>
        /// HMAC-SHA256 簽章（涵蓋以上四個欄位）
        /// </summary>
        public string Signature { get; set; } = string.Empty;
    }
}