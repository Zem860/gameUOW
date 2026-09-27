using System.Security.Cryptography;
namespace Game.Application.Utils
{
    /// <summary>
    /// 產生票券用的一次性隨機值（Nonce）
    /// </summary>
    public static class NonceGenerator
    {
        /// <summary>
        /// 隨機位元組長度（16 bytes = 128 bits，碰撞機率可忽略）
        /// </summary>
        private const int NonceByteLength = 16;
        /// <summary>
        /// 產生密碼學安全的隨機值，並以 URL-safe Base64 編碼（無 padding，長度 22）
        /// </summary>
        /// <returns>Nonce 字串</returns>
        public static string Generate()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(NonceByteLength);
            // 標準 Base64 的 + / = 在 URL 或 JSON 中容易出問題，換成 - _ 並去掉結尾的 =
            return Convert
                .ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}