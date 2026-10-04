using Game.Domain.Entities;

namespace Game.Abstractions.Repositories
{
    /// <summary>
    /// 遊戲結果的專用查詢（泛型 Repository 無法表達的排序、筆數限制）；查詢走讀取節點（secondary）
    /// </summary>
    /// <remarks>
    /// <para>分工：簡單的通用讀取（找一筆、算數量）用 IReadRepository；只有特定 Entity 需要的複雜查詢放這裡，
    /// 不把排序、筆數參數加進泛型介面，避免每個 Entity 都多出用不到的方法。</para>
    /// <para>排序與筆數限制在 Mongo 伺服器上執行，只傳回需要的筆數；
    /// 若改用 IReadRepository.GetListAsync，會把該遊戲所有成績搬回來再在程式裡排序。</para>
    /// <para>只負責讀取，不經過 IUnitOfWork（UoW 只管寫入的交易）。</para>
    /// </remarks>
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