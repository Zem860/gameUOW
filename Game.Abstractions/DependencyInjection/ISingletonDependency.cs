namespace Game.Abstractions.DependencyInjection
{
    /// <summary>
    /// 標記類別為 Singleton 生命週期（整個應用程式只有一個實例）
    /// </summary>
    /// <remarks>
    /// 實作此介面的類別將由慣例註冊自動加入 DI 容器。
    /// 適用於無狀態且執行緒安全的服務（例：HmacService、NonceGenerator）。
    /// 注意：Singleton 不可依賴 Scoped 服務（Captive Dependency）。
    /// </remarks>
    public interface ISingletonDependency
    {
    }
}