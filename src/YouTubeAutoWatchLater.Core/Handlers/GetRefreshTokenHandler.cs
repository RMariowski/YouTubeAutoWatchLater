using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.Google;

namespace YouTubeAutoWatchLater.Core.Handlers;

public interface IGetRefreshTokenHandler
{
    Task<string> HandleAsync();
}

internal sealed class GetRefreshTokenHandler : IGetRefreshTokenHandler
{
    private readonly IGoogleApi _googleApi;
    private readonly ILogger<GetRefreshTokenHandler> _logger;

    public GetRefreshTokenHandler(IGoogleApi googleApi, ILogger<GetRefreshTokenHandler> logger)
    {
        _googleApi = googleApi;
        _logger = logger;
    }

    public async Task<string> HandleAsync()
    {
        _logger.LogInformation("Starting authorization");
        var credentials = await _googleApi.AuthorizeAsync();
        _logger.LogInformation("Authorization finished");

        return credentials.Token.RefreshToken;
    }
}
