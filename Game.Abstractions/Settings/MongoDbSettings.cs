namespace Game.Abstractions.Settings
{
    /// <summary>
    /// MongoDB 設定
    /// </summary>
    public class MongoDbSettings
    {
        /// <summary>
        /// 設定節點名稱
        /// </summary>
        public const string SectionName = "MongoDb";

        /// <summary>
        /// 連線字串（機密：本機放 appsettings.Development.local.json，部署用環境變數 MongoDb__ConnectionString）
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        /// <summary>
        /// 資料庫名稱
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

    }
}