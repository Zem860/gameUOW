using Game.Abstractions.Common;
using Game.Abstractions.Dtos.Game;
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

        /// <summary>
        /// 建立遊戲 API 控制器
        /// </summary>
        /// <param name="gameService">遊戲流程服務</param>
        public GamesController(IGameService gameService)
        {
            _gameService = gameService;
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
    }
}