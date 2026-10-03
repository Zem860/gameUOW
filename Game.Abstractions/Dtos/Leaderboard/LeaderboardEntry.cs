namespace Game.Abstractions.Dtos.Leaderboard
{
    /// <summary>
    /// 排行榜上的一筆成績
    /// </summary>
    public sealed class LeaderboardEntry
    {
        /// <summary>
        /// 遊戲結果 Id（Redis Sorted Set 的成員；每局獨立一筆，同一玩家可出現多次）
        /// </summary>
        public string GameResultId { get; set; } = string.Empty;

        /// <summary>
        /// 玩家名字
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// 分數
        /// </summary>
        public int Score { get; set; }
    }
}