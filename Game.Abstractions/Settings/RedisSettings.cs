namespace Game.Abstractions.Settings
{
    /// <summary>
    /// Redis設定
    /// </summary>
    public class RedisSettings
    {
        /// <summary>
        /// 設定節點名稱
        /// </summary>
        public const string SectionName = "Redis";
        
        /// <summary>
        /// 連線字串(本機：localhost:6379；部署用環境變數 Redis__ConnectionString)
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

    }
}