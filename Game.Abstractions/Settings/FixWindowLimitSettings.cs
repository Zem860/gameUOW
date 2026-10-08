namespace Game.Abstractions.Settings
{
    /// <summary>
    /// 單一 policy 的固定視窗限流設定
    /// </summary>
    public class FixWindowLimitSettings
    {
        /// <summary>
        /// 一個視窗內最多允許幾次請求
        /// </summary>
        public int PermitLimt { get; set; }
        /// <summary>
        /// 視窗長度（秒）
        /// </summary>
        public int WindowLength { get; set; }
    }

}