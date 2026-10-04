using Game.Abstractions.Dtos.Leaderboard;

namespace Game.Abstractions.IApplication
{
    /// <summary>
    /// 排行榜服務
    /// </summary>
    public interface ILeaderboardService
    {
        /// <summary>
        /// 取得指定遊戲分數最高的前 N 筆：先讀快取，快取沒有資料時從資料庫查詢並補回快取
        /// </summary>
        /// <param name="code">遊戲代碼（例如 snake）</param>
        /// <param name="count">筆數（1～100）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>前 N 筆成績（分數由高到低）；沒有任何成績時回傳空集合</returns>
        /// <exception cref="KeyNotFoundException">遊戲不存在或已停用</exception>
        Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string code, int count, CancellationToken cancellationToken = default);

    }
}