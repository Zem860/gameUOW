using Game.Abstractions.Dtos.Leaderboard;
namespace Game.Abstractions.Caching
{
    /// <summary>
    /// 排行榜快取
    /// </summary>
    public interface ILeaderboardCache
    {
        /// <summary>
        /// 加入一筆成績
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="entry">成績</param>
        Task AddAsync(string gameId, LeaderboardEntry entry);
        /// <summary>
        /// 取得分數最高的前 N 筆（分數由高到低）；快取沒有資料時回傳空集合
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="count">筆數</param>
        Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string gameId, int count);
    }
}