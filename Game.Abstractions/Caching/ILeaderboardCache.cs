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
        /// <remarks>
        /// 只讀快取，不會去讀資料庫；快取是空的時候，由呼叫端改用 IGameResultQueryExecutor.GetTopScoresAsync 查資料庫
        /// </remarks>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="count">筆數</param>
        Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string gameId, int count);

        /// <summary>
        /// 整份放入（覆蓋舊的）：從資料庫查到完整清單後補回快取用
        /// </summary>
        /// <param name="gameId">遊戲 Id</param>
        /// <param name="entries">完整清單（分數由高到低）</param>
        Task SetAllAsync(string gameId, IReadOnlyList<LeaderboardEntry> entries);
    }
}