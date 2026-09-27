using Game.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 本機機密（連線字串）放在 gitignore 的檔案，只在開發環境載入；
// 一定要在 AddMongoDbServices 之前，因為它會立刻讀取設定並檢查
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
}

// Add services to the container.
// 以下是透過擴充方法註冊服務抓取appsetting的設定並開放或注入給di的其他服務使用，方便維護與測試
builder.Services.AddMongoDbServices(builder.Configuration);
builder.Services.AddHmacServices(builder.Configuration);
builder.Services.AddConventionalServices();
 // 系統時鐘：服務一律用 TimeProvider.GetUtcNow() 取時間，測試時可換成假時鐘
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
// 建立索引與種子資料，成功後才開始接收 Request
await app.InitializeDatabaseAsync();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();