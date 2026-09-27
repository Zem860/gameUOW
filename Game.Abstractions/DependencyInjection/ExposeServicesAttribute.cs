namespace Game.Abstractions.DependencyInjection
{
    /// <summary>
    /// 指定慣例註冊時，只用哪些介面把類別登記進 DI
    /// </summary>
    /// <remarks>
    /// <para>
    /// 沒貼標籤：類別實作的所有介面（扣掉 IScopedDependency 等標記介面）都會登記，類別本身也會登記。
    /// 例：UnitOfWork 實作 IUnitOfWork、IDisposable，兩個都會被登記，但 IDisposable 不該被注入。
    /// </para>
    /// <para>
    /// 貼了標籤：只登記建構子參數列出的介面。
    /// </para>
    /// <para>
    /// 標籤本身不做任何事，只是資料：Game.Web 的 AddConventionalServices() 啟動掃描時用反射讀取，
    /// 只在登記時讀一次。類別仍要實作 IScopedDependency 等標記介面才會被掃到，標籤只決定「用哪個介面登記」。
    /// </para>
    /// <para>
    /// AttributeUsage：只能貼在類別上（貼在方法、屬性上會編譯錯誤），且同一個類別只能貼一次。
    /// </para>
    /// <para>使用位置：Game.Infrastructure/MongoDb/UnitOfWork.cs</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [ExposeServices(typeof(IUnitOfWork))]
    /// public class UnitOfWork : IUnitOfWork, IDisposable, IScopedDependency
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExposeServicesAttribute : Attribute
    {
        /// <summary>
        /// 要登記的服務介面（例：typeof(IUnitOfWork)）
        /// </summary>
        public Type[] ServiceTypes { get; }

        /// <summary>
        /// 是否也用類別本身登記（保留欄位，註冊程式目前未讀取）
        /// </summary>
        public bool IncludeSelf { get; set; }

        /// <summary>
        /// 是否也登記預設會登記的介面（保留欄位，註冊程式目前未讀取）
        /// </summary>
        public bool IncludeDefaults { get; set; }

        /// <summary>
        /// 建立標籤
        /// </summary>
        /// <param name="serviceTypes">要登記的服務介面，可傳多個</param>
        public ExposeServicesAttribute(params Type[] serviceTypes)
        {
            ServiceTypes = serviceTypes ?? Array.Empty<Type>();
        }
    }
}
