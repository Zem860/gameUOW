namespace Game.Abstractions.IApplication.Security
{
    /// <summary>
    /// HMAC-SHA256 簽章服務（密鑰來自 HmacSettings.Secret）
    /// </summary>
    public interface IHmacService
    {          
        /// <summary>
        /// 對字串計算 HMAC-SHA256 簽章
        /// </summary>
        /// <param name="data">要簽章的內容（呼叫端負責組成固定格式）</param>
        /// <returns>簽章（64 字元大寫十六進位）</returns>
        string Sign(string data);
        /// <summary>
        /// 驗證簽章是否與內容相符（固定時間比對，避免計時攻擊）
        /// </summary>
        /// <param name="data">要驗證的內容</param>
        /// <param name="signature">收到的簽章</param>
        /// <returns>相符回傳 true；不符或格式錯誤回傳 false（不拋例外）</returns>
        bool Verify(string data, string signature);
    }
}