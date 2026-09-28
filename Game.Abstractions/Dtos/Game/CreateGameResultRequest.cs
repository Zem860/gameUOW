using System.ComponentModel.DataAnnotations;

namespace Game.Abstractions.Dtos.Game
{
    /// <summary>
    /// 儲存遊戲結果請求：開始遊戲時拿到的票券（原封不動）+ 玩家名字 + 分數
    /// </summary>
    public sealed class CreateGameResultRequest
    {
        /// <summary>
        /// 票券：遊戲結果識別碼
        /// </summary>
        [Required]
        public string GameResultId { get; set; } = string.Empty;

        /// <summary>
        /// 票券：遊戲識別碼
        /// </summary>
        [Required]
        public string GameId { get; set; } = string.Empty;

        /// <summary>
        /// 票券：開始時間
        /// </summary>
        public long StartedAt { get; set; }

        /// <summary>
        /// 票券：一次隨機值（其實沒什麼用練習而已）
        /// </summary>
        [Required]
        public string Nonce { get; set; } = string.Empty;

        /// <summary>
        /// 票券：HMAC-SHA256 簽章
        /// </summary>
        [Required]
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        /// 玩家名稱
        /// </summary>
        [Required]
        [StringLength(10)]
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// 分數（不可為負數）
        /// </summary>
        [Range(0, int.MaxValue)]
        public int Score { get; set; }
    }
}