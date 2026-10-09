namespace Game.Abstractions.Settings
{
    /// <summary>
    /// 單一 policy 的固定視窗限流設定
    /// </summary>
    public class FixedWindowLimitSettings
    {
        /// <summary>
        /// 一個視窗內最多允許幾次請求
        /// </summary>
        public int PermitLimit { get; set; }
        /// <summary>
        /// 視窗長度（秒）
        /// </summary>
        public int WindowSeconds { get; set; }
    }

}