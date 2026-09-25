using Game.Domain.Entities;

namespace Game.Abstractions.Repositories
{
    /// <summary>
    /// 遊戲資料存取（對應 MongoDB <c>games</c> collection）
    /// </summary>
    public interface IGameRepository
    {
        /// <summary>
        /// 以遊戲代碼查詢遊戲（例：snake）；找不到回傳 null
        /// </summary>
        Task<GameInfo?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
        /// <summary>
        /// 以遊戲 Id 查詢遊戲；找不到回傳 null
        /// </summary>
        Task<GameInfo?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
    }
}