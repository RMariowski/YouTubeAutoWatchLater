using MediatR;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Application.Google;

namespace YouTubeAutoWatchLater.Application.Handlers;

public sealed  class GetRefreshToken
{
    public sealed record Query : IRequest<string>;

    public sealed class Handler : IRequestHandler<Query, string>
    {
        private readonly IGoogleApi _googleApi;
        private readonly ILogger<Handler> _logger;

        public Handler(IGoogleApi googleApi, ILogger<Handler> logger)
        {
            _googleApi = googleApi;
            _logger = logger;
        }

        public async Task<string> Handle(Query request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting authorization");
            var credentials = await _googleApi.Authorize();
            _logger.LogInformation("Authorization finished");

            return credentials.Token.RefreshToken;
        }
    }
}
