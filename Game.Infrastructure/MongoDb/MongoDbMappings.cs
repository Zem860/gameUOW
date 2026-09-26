using Game.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace Game.Infrastructure.MongoDb
{
    /// <summary>
    /// MongoDB 對應設定：定義 Entity 與 BSON 文件之間的轉換規則
    /// </summary>
    /// <remarks>
    /// 只負責 mapping（C# 物件 ↔ Mongo 文件的格式），不連線、不讀寫任何資料。
    /// 必須在第一次使用 MongoClient 之前呼叫 <see cref="Register"/>。
    /// Driver 的註冊是全域的，且同一型別重複註冊會拋出例外，因此以旗標確保只執行一次。
    /// </remarks>
    public static class MongoDbMappings
    {
        private static readonly object _lock = new();
        private static bool _registered;

        /// <summary>
        /// 註冊命名慣例、序列化器與 class map（重複呼叫不會有影響）
        /// </summary>
        public static void Register()
        {
            lock (_lock)
            {
                if (_registered)
                {
                    return;
                }

                // 命名慣例：欄位名稱轉成 camelCase；讀取時忽略 C# 類別沒有的欄位
                ConventionPack conventions =
                [
                    new CamelCaseElementNameConvention(),
                    new IgnoreExtraElementsConvention(true),
                ];
                ConventionRegistry.Register(
                    "GameConventions",
                    conventions,
                    type => type.Namespace == typeof(GameInfo).Namespace);

                // DateTimeOffset 存成 Mongo 原生 Date（UTC，精度到毫秒）
                BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.DateTime));

                // games：C# 用 string，Mongo 存 ObjectId；Id 為空時由 Driver 產生
                BsonClassMap.RegisterClassMap<GameInfo>(classMap =>
                {
                    classMap.AutoMap();
                    classMap.MapIdMember(x => x.Id)
                        .SetSerializer(new StringSerializer(BsonType.ObjectId))
                        .SetIdGenerator(StringObjectIdGenerator.Instance);
                });

                // gameResults：_id 是 /start 產生的 GUID 字串，維持 string；GameId 對應 games._id，存 ObjectId
                BsonClassMap.RegisterClassMap<GameResult>(classMap =>
                {
                    classMap.AutoMap();
                    classMap.MapMember(x => x.GameId)
                        .SetSerializer(new StringSerializer(BsonType.ObjectId));
                });

                _registered = true;
            }
        }
    }
}
