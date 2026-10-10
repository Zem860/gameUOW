# ---------- 第一段：編譯（SDK image，有編譯器，很大） ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 先只複製 csproj 並還原套件：套件沒變時，這一層直接用快取
COPY Game.Domain/Game.Domain.csproj Game.Domain/
COPY Game.Abstractions/Game.Abstractions.csproj Game.Abstractions/
COPY Game.Application/Game.Application.csproj Game.Application/
COPY Game.Infrastructure/Game.Infrastructure.csproj Game.Infrastructure/
COPY Game.Web/Game.Web.csproj Game.Web/
RUN dotnet restore Game.Web/Game.Web.csproj
# 再複製全部原始碼並發佈
COPY . .
RUN dotnet publish Game.Web/Game.Web.csproj -c Release -o /app/publish --no-restore
# ---------- 第二段：執行（只有 runtime，小很多） ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# 用內建(dotnet線上他們自己設定好的帳號)的非 root 使用者執行
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Game.Web.dll"]