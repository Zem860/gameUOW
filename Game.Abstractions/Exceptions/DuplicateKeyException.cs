namespace Game.Abstractions.Exceptions
{
    /// <summary>
    /// 資料重複：寫入時違反唯一鍵（例如同一張票券重複存檔）
    /// </summary>
    /// <remarks>
    /// 由資料存取層把資料庫特有的例外轉成這個型別，上層不需知道底層用哪種資料庫。
    /// </remarks>
    public class DuplicateKeyException : Exception
    {
        /// <summary>
        /// 建立資料重複例外
        /// <param name="innerException">原始的資料庫例外（保留給 log 追查）</param>
        public DuplicateKeyException(string message, Exception innerException):base(message, innerException)
        {
        }
    }
}