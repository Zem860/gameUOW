namespace Game.Abstractions.DependencyInjection
{
    /// <summary>
    /// 明確指定要暴露的服務介面
    /// </summary>
    /// <remarks>
    /// 預設情況下，實作依賴介面的類別會註冊為其實作的所有介面。
    /// 使用此屬性可以明確指定只註冊特定的介面。
    /// </remarks>
    public class ExposeServiceAttribute : Attribute
    {
        /// <summary>
        /// 要暴露的服務類型陣列
        /// </summary>
        public Type[] ServiceTypes { get; }

        /// <summary>
        /// 是否包含自身類型
        /// </summary>
        public bool IncludeSelf { get; set; }

        /// <summary>
        /// 是否包含預設介面
        /// </summary>
        public bool IncludeDefaults { get; set; }


        /// <summary>
        /// 建立服務暴露屬性
        /// </summary>
        /// <param name="serviceTypes">要暴露的服務型別</param>
        public ExposeServiceAttribute(params Type[] serviceTypes)
        {
            ServiceTypes = serviceTypes ?? Array.Empty<Type>();
        }
    }
}