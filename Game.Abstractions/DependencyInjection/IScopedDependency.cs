namespace Game.Abstractions.DependencyInjection

{
    /// <summary>
    /// 標記為Scoped生命週期(同一HTTP請求內共享實例)
    /// </summary>
    /// <remarks>
    /// 實作此介面的類別將由慣例註冊自動加入 DI 容器。
    /// 適用於 Repository、UnitOfWork、Application Service。
    /// </remarks>
    public interface IScopedDependency
    {
    }
}