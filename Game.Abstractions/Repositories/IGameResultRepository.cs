using Game.Domain.Entities;

namespace Game.Abstractions.Repositories
{
    /// <summary>
    /// 遊戲結果資料存取（對應 MongoDB <c>gameResults</c> collection）
    /// </summary>
    public interface IGameResultRepository
    {
        /// <summary>
        /// 新增一筆遊戲結果；_id 或 nonce 重複時會拋出例外（防重複兌換）
        /// </summary>
        Task InsertAsync(GameResult gameResult, CancellationToken cancellationToken = default);
        /// <summary>
        /// 取得指定遊戲最高分數的前N筆 （Redis 故障時的排行榜來源）
        /// </summary>
        Task<IReadOnlyList<GameResult>> GetTopScoreAsync(string gameId, int count, CancellationToken cancellationToken = default);
    }

}