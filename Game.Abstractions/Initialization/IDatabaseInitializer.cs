namespace Game.Abstractions.Initialization
{
    /// <summary>
    /// 資料庫初始化：啟動時建立索引與種子資料
    /// </summary>
    /// <remarks>
    /// 使用位置：Game.Web 的 InitializeDatabaseAsync() 在程式啟動、開始接 Request 之前呼叫一次。
    /// 必須可以重複執行（每次啟動都會跑）：索引已存在不會重建，種子資料已存在不會重複新增。
    /// 失敗時應拋出例外，讓程式啟動失敗。
    /// </remarks>
    public interface IDatabaseInitializer
    {
        /// <summary>
        /// 建立索引與種子資料
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}