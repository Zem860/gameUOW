using Game.Abstractions.Common;
using Game.Abstractions.Dtos.Game;
using Game.Abstractions.Dtos.Leaderboard;
using Game.Abstractions.IApplication;
using Microsoft.AspNetCore.Mvc;

namespace Game.Web.Controllers
{
    /// <summary>
    /// 遊戲 API
    /// </summary>
    [Route("api/games")]
    public sealed class GamesController : BaseController
    {
        private readonly IGameService _gameService;
        private readonly ILeaderboardService _leaderboardService;

        /// <summary>
        /// 建立遊戲 API 控制器
        /// </summary>
        /// <param name="gameService">遊戲流程服務</param>
        /// <param name="leaderboardService">排行榜服務</param>
        public GamesController(IGameService gameService, ILeaderboardService leaderboardService)
        {
            _gameService = gameService;
            _leaderboardService = leaderboardService;
        }

        /// <summary>
        /// 開始遊戲，取得遊戲票券（不寫入資料庫）
        /// </summary>
        /// <param name="code">遊戲代碼（例如 snake）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>遊戲票券</returns>
        [HttpPost("{code}/start")]
        [ProducesResponseType(typeof(ApiResponse<StartGameResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<StartGameResponse>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<StartGameResponse>>> Start(string code, CancellationToken cancellationToken)
        {
            StartGameResponse ticket = await _gameService.StartAsync(code, cancellationToken);
            return BusinessOkResponse(ticket, "遊戲開始");
        }

        /// <summary>
        /// 取得排行榜：分數最高的前 N 筆（分數由高到低）
        /// </summary>
        /// <param name="code">遊戲代碼（例如 snake）</param>
        /// <param name="count">筆數（1～100，未帶時預設 10）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>排行榜成績；沒有任何成績時回傳空陣列</returns>
        [HttpGet("{code}/leaderboard")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaderboardEntry>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaderboardEntry>>),
StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaderboardEntry>>), StatusCodes.Status404NotFound)]

        public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaderboardEntry>>>> GetLeaderboard(string code, [FromQuery] int count = 10, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<LeaderboardEntry> entries = await _leaderboardService.GetTopAsync(code, count, cancellationToken);
            return BusinessOkResponse(entries, "排行榜");
        }
    }
}