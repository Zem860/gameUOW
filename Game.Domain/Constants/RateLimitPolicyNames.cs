namespace Game.Domain.Constants
{
      /// <summary>
      /// 請求限流 policy 名稱（必須與 AddRateLimiter 註冊及 [EnableRateLimiting] 使用的字串一致）
      /// </summary>
      public static class RateLimitPloicyNames
    {
        public const string GameStart = "game-start";
        /// <summary>儲存遊戲結果（依 IP 分組計數）</summary>
        public const string GameResult = "game-result";
    }
}