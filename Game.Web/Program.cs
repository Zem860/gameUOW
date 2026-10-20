// EasterEgg -> 1/9【啟動】註冊驗證與授權（搜尋「EasterEgg -> 1」找到相關的兩處）
// 下一步 -> 2：Extensions/AuthExtensions.cs
using Game.Web.Extensions;
using Game.Web.Middlewares;

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
builder.Services.AddRedisServices(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddHmacServices(builder.Configuration);
builder.Services.AddRateLimitingServices(builder.Configuration);
builder.Services.AddCorsServices(builder.Configuration);
// EasterEgg -> 1：註冊假登入方案、彩蛋 Policy、授權 Handler（內容見 2）
builder.Services.AddAuthServices();
builder.Services.AddConventionalServices();
// 系統時鐘：服務一律用 TimeProvider.GetUtcNow() 取時間，測試時可換成假時鐘
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddControllers().AddApiResponseValidation();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// 最外層：攔截後續所有中介軟體與 Controller 拋出的例外，統一轉成 ApiResponse
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// 建立索引與種子資料，成功後才開始接收 Request
await app.InitializeDatabaseAsync();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseRateLimiter();

// 先知道你是誰（填好 HttpContext.User），再判斷你能不能呼叫；順序不能反
// EasterEgg -> 1：請求進來時，這行會執行 5 的假登入
app.UseAuthentication();
// EasterEgg -> 1：這行會讀 6 的標籤，用 7、8 判斷能不能進 9
app.UseAuthorization();

app.MapControllers();

app.Run();