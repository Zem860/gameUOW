namespace Game.Abstractions.DependencyInjection
{
    /// <summary>
    /// 標記類別為 Transient 生命週期（每次注入都建立新實例）
    /// </summary>
    /// <remarks>
    /// 實作此介面的類別將由慣例註冊自動加入 DI 容器。
    /// 適用於無狀態、輕量的服務。
    /// </remarks>
    public interface ITransientDependency
    {
    }
}