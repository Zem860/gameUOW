using System.Globalization;
using Game.Abstractions.DependencyInjection;
using Game.Abstractions.Dtos.Game;
using Game.Abstractions.IApplication;
using Game.Abstractions.IApplication.Security;
using Game.Abstractions.Repositories;
using Game.Application.Utils;
using Game.Domain.Entities;

namespace Game.Application.Services.Games
{
    /// <summary>
    /// 遊戲流程服務
    /// </summary>
    [ExposeServices(typeof(IGameService))]
    public sealed class GameService : IGameService, IScopedDependency
    {
        private readonly IReadRepository<GameInfo> _gameRepository;
        private readonly IHmacService _hmacService;
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// 建立遊戲流程服務
        /// </summary>
        /// <param name="gameRepository">遊戲唯讀 Repository</param>
        /// <param name="hmacService">HMAC 簽章服務</param>
        /// <param name="timeProvider">系統時鐘</param>
        public GameService(IReadRepository<GameInfo> gameRepository, IHmacService hmacService, TimeProvider timeProvider)
        {
            _gameRepository = gameRepository;
            _hmacService = hmacService;
            _timeProvider = timeProvider;
        }

        /// <inheritdoc />
        public async Task<StartGameResponse> StartAsync(string code, CancellationToken cancellationToken = default)
        {
            GameInfo? game = await _gameRepository.FirstOrDefaultAsync(game => game.Code == code && game.IsActive, cancellationToken);
            if (game is null)
            {
                throw new KeyNotFoundException($"game not found: {code}");
            }

            string gameResultId = Guid.NewGuid().ToString();
            long startedAt = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
            string nonce = NonceGenerator.Generate();
            string signature = _hmacService.Sign(BuildTicketPayload(gameResultId, game.Id, startedAt, nonce));
            return new StartGameResponse
            {
                GameResultId = gameResultId,
                GameId = game.Id,
                StartedAt = startedAt,
                Nonce = nonce,
                Signature = signature,
            };
        }

        /// <summary>
        /// 組出票券的簽章內容；簽發與驗證必須用同一個方法，欄位順序與格式才會一致
        /// </summary>
        private static string BuildTicketPayload(string gameResultId, string gameId, long startedAt, string nonce)
        {
            return string.Join(
                '|',
                gameResultId,
                gameId,
                startedAt.ToString(CultureInfo.InvariantCulture),
                nonce);
        }
    }
}
