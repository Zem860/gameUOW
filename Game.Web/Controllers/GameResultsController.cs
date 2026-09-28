using Game.Abstractions.Common;
using Game.Abstractions.Dtos.Game;
using Game.Abstractions.IApplication;
using Microsoft.AspNetCore.Mvc;

namespace Game.Web.Controllers
{
    /// <summary>
    /// 遊戲結果 API
    /// </summary>
    [Route("api/game-results")]
    public sealed class GameResultsController : BaseController
    {
        private readonly IGameService _gameService;

        /// <summary>
        /// 建立遊戲結果 API 控制器
        /// </summary>
        /// <param name="gameService">遊戲流程服務</param>
        public GameResultsController(IGameService gameService)
        {
            _gameService = gameService;
        }

        /// <summary>
        /// 儲存遊戲結果：驗證票券後寫入
        /// </summary>
        /// <param name="request">票券 + 玩家名字 + 分數</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>已儲存的遊戲結果識別碼</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateGameResultResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreateGameResultResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<CreateGameResultResponse>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<CreateGameResultResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<CreateGameResultResponse>), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse<CreateGameResultResponse>>> Create(
            CreateGameResultRequest request,
            CancellationToken cancellationToken)
        {
            CreateGameResultResponse result = await _gameService.FinishAsync(request, cancellationToken);
            return BusinessOkResponse(result, "儲存成功");
        }
    }
}
