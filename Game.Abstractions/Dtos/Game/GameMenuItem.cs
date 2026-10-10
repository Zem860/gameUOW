namespace Game.Abstractions.Dtos.Game
{
    /// <summary>
    /// 遊戲菜單上的一款遊戲
    /// </summary>
    public sealed class GameMenuItem
    {
        /// <summary>
        /// 遊戲代碼（呼叫 start / leaderboard 時放在網址上，例如 snake）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 遊戲名稱（顯示在菜單上）
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
    }
}