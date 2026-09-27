using Game.Domain.Entities;

namespace Game.Abstractions.Repositories
{
    /// <summary>
    /// 遊戲結果的專用查詢（泛型 Repository 無法表達的排序、筆數限制）；查詢走讀取節點（secondary）
    /// </summary>
    public interface IGameResultQueryExecutor
    {
        /// <summary>
        /// 取得指定遊戲分數最高的前 N 筆（分數由高到低）；Redis 故障時的排行榜來源
        /// </summary>
        Task<IReadOnlyList<GameResult>> GetTopScoresAsync(
            string gameId,
            int count,
            CancellationToken cancellationToken = default);
    }
}