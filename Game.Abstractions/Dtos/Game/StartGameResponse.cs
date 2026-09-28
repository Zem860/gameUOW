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
        /// 一次性隨機值；公開值，不是秘密
        /// </summary>
        /// <remarks>
        /// 防重複送出的第二層：存檔時 gameResults.nonce 唯一索引擋下重複（主防線是 GameResultId 的 _id 唯一鍵）。
        /// 不需另外保存或比對：是否被竄改由簽章檢查，是否用過由唯一索引檢查。
        /// 前端只原封不動帶回，不會拿它做任何證明，因此不具挑戰（challenge）作用；
        /// 若之後改成要求前端對 nonce 簽章（挑戰-回應），它才會成為證明「當下持有金鑰」的必要欄位。
        /// </remarks>
        public string Nonce { get; set; } = string.Empty;

        /// <summary>
        /// HMAC-SHA256 簽章（涵蓋以上四個欄位）
        /// </summary>
        /// <remarks>
        /// 伺服器不保存簽章：驗證時用同一把密鑰對收到的四個欄位重算，再固定時間比對。
        /// 任一欄位被改，重算結果就對不上；沒有密鑰則無法產生合法簽章。
        /// </remarks>
        public string Signature { get; set; } = string.Empty;
    }
}