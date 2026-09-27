using System.Security.Cryptography;
using System.Text;
using Game.Abstractions.DependencyInjection;
using Game.Abstractions.IApplication.Security;
using Game.Abstractions.Settings;
using Microsoft.Extensions.Options;

namespace Game.Application.Services.Security
{
    /// <summary>
    /// HMAC-SHA256 簽章與驗證
    /// </summary>
    [ExposeServices(typeof(IHmacService))]
    public sealed class HmacService : IHmacService, ISingletonDependency
    {
        private readonly byte[] _keyBytes;
        /// <summary>
        /// 建立 HMAC 簽章服務
        /// </summary>
        /// <param name="options">HMAC 設定（密鑰）</param>
        public HmacService(IOptions<HmacSettings> options)
        {
            // 密鑰不會變，建構時轉成 bytes 一次，之後每次簽章直接使用
            _keyBytes = Encoding.UTF8.GetBytes(options.Value.Secret);
        }

        /// <inheritdoc />
        public string Sign(string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] hashBytes = HMACSHA256.HashData(_keyBytes, dataBytes);
            return Convert.ToHexString(hashBytes);
        }

        /// <inheritdoc />
        public bool Verify(string data, string signature)
        {
            if (string.IsNullOrEmpty(signature))
            {
                return false;
            }

            string expectedSignature = Sign(data);
            // 不用 ==：== 遇到第一個不同字元就停，回應時間會洩漏猜對了幾個字元
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signature));
        }
    }
}