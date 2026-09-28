namespace Game.Abstractions.Common
{
    /// <summary>
    /// API 統一回應格式
    /// </summary>
    /// <typeparam name="T">回應資料型別</typeparam>
    public sealed class ApiResponse<T>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 回應訊息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 回應資料（失敗時為 null）
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 錯誤代碼（成功時為 null）
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 建立成功回應
        /// </summary>
        /// <param name="data">回應資料</param>
        /// <param name="message">回應訊息</param>
        /// <returns>API 回應物件</returns>
        public static ApiResponse<T> Ok(T? data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
            };
        }

        /// <summary>
        /// 建立失敗回應
        /// </summary>
        /// <param name="message">錯誤訊息</param>
        /// <param name="errorCode">錯誤代碼</param>
        /// <returns>API 回應物件</returns>
        public static ApiResponse<T> Error(string message, string? errorCode = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
            };
        }
    }
}