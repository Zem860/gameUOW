namespace Game.Abstractions.Dtos.Game
{
    /// <summary>
    /// 儲存遊戲結果回應
    /// </summary>
    public sealed class CreateGameResultResponse
    {
        /// <summary>
        /// 已儲存的遊戲結果識別碼（與票券上的 GameResultId 相同）
        /// </summary>
        public string GameResultId { get; set; } = string.Empty;
    }
}