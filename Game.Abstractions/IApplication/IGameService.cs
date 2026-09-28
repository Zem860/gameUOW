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
        /// <summary>
        /// 儲存遊戲結果：驗證票券 → 檢查名字與分數 → 寫入資料庫
        /// </summary>
        /// <param name="request">票券 + 玩家名字 + 分數</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>已儲存的遊戲結果識別碼</returns>
        Task<CreateGameResultResponse> FinishAsync(
            CreateGameResultRequest request,
            CancellationToken cancellationToken = default);
    }
}