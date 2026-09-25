namespace Game.Domain.Entities
{
    /// <summary>
    /// 遊戲
    /// </summary>
    public class GameInfo
    {
        /// <summary>
        /// 遊戲 Id
        /// </summary>
        public string Id {get; set;} = string.Empty;

        /// <summary>
        /// 遊戲代碼
        /// </summary>
        public string Code {get; set;} = string.Empty;

        /// <summary>
        /// 遊戲名稱
        /// </summary>
        public string DisplayName {get; set;} = string.Empty;

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsActive {get; set;} = true;

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTimeOffset CreatedAt {get; set;} = DateTimeOffset.UtcNow;
    }
}