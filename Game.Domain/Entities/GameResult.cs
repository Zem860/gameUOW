  namespace Game.Domain.Entities
  {
      /// <summary>
      /// 遊戲結果
      /// </summary>
      public class GameResult
      {
          /// <summary>
          /// 結果 Id（= 票券上的 gameRunId，同時為 MongoDB _id）
          /// </summary>
          public string Id {get; set;} = string.Empty;

          /// <summary>
          /// 遊戲 Id（對應 <see cref="GameInfo.Id"/>）
          /// </summary>
          public string GameId {get; set;} = string.Empty;

          /// <summary>
          /// 玩家名稱
          /// </summary>
          public string PlayerName {get; set;} = string.Empty;

          /// <summary>
          /// 玩家分數
          /// </summary>
          public int Score {get; set;}

          /// <summary>
          /// 遊戲開始時間（Server 於 /start 產生）
          /// </summary>
          public DateTimeOffset StartedAt {get; set;}

          /// <summary>
          /// 遊戲結束時間（Server 於 /finish 產生）
          /// </summary>
          public DateTimeOffset FinishedAt {get; set;}

          /// <summary>
          /// 遊玩時間（秒），由 StartedAt、FinishedAt 計算
          /// </summary>
          public int DurationSeconds => (int)(FinishedAt - StartedAt).TotalSeconds;

          /// <summary>
          /// 票券隨機值
          /// </summary>
          public string Nonce {get; set;} = string.Empty;

          /// <summary>
          /// 紀錄建立時間
          /// </summary>
          public DateTimeOffset CreatedAt {get; set;}
      }
  }
