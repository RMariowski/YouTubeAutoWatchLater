using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Application.Google;
using YouTubeAutoWatchLater.Application.Settings;
using YouTubeAutoWatchLater.Application.YouTube;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Azure;

public class YouTubeAutoWatchLater
{
    private readonly YouTubeAutoWatchLaterHandler _youTubeAutoWatchLaterHandler;
    private readonly IGoogleApi _googleApi;
    private readonly IPlaylistItemRepository _playlistItemRepository;
    private readonly ISettings _settings;
    private readonly ILogger<YouTubeAutoWatchLater> _logger;

    public YouTubeAutoWatchLater(IGoogleApi googleApi, ISettings settings, ILogger<YouTubeAutoWatchLater> logger,
        IPlaylistItemRepository playlistItemRepository, YouTubeAutoWatchLaterHandler youTubeAutoWatchLaterHandler)
    {
        _googleApi = googleApi;
        _settings = settings;
        _logger = logger;
        _playlistItemRepository = playlistItemRepository;
        _youTubeAutoWatchLaterHandler = youTubeAutoWatchLaterHandler;
    }

    [Singleton]
    [FunctionName(nameof(Run))]
    public async Task Run(
        [TimerTrigger("%Cron%"
#if DEBUG
            , RunOnStartup = true
#endif
        )]
        TimerInfo timerInfo)
    {
        await _youTubeAutoWatchLaterHandler.Handle();
    }

    [Singleton]
    [FunctionName(nameof(DeletePrivatePlaylistItems))]
    public async Task DeletePrivatePlaylistItems(
        [TimerTrigger("%DeletePrivatePlaylistItemsCron%"
#if DEBUG
            , RunOnStartup = true
#endif
        )]
        TimerInfo timerInfo)
    {
        await _playlistItemRepository.DeletePrivatePlaylistItemsOfPlaylist(_settings.PlaylistId);
    }

    [Singleton]
    [FunctionName(nameof(GetRefreshToken))]
    public async Task<IActionResult> GetRefreshToken(
        [HttpTrigger(AuthorizationLevel.Function, "get")]
        HttpRequest request)
    {
        _logger.LogInformation("Starting authorization");
        var credentials = await _googleApi.Authorize();
        _logger.LogInformation("Authorization finished");

        return new OkObjectResult(credentials.Token.RefreshToken);
    }
}
