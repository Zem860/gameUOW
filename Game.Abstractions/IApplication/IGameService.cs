using Game.Abstractions.Dtos.Game;

namespace Game.Abstractions.IApplication
{
    /// <summary>
    /// 遊戲流程服務（開始遊戲、儲存結果）
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// 開始遊戲：確認遊戲存在且啟用，簽發遊戲票券（不寫入資料庫）
        /// </summary>
        /// <param name="code">遊戲代碼（例如 snake）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>遊戲票券</returns>
        Task<StartGameResponse> StartAsync(string code, CancellationToken cancellationToken = default);
    }
}