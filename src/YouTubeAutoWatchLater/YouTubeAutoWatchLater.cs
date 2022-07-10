using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Repositories.Configuration;
using YouTubeAutoWatchLater.YouTube;

namespace YouTubeAutoWatchLater;

public class YouTubeAutoWatchLater
{
    private readonly IYouTubeService _youTubeService;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILogger<YouTubeAutoWatchLater> _logger;

    public YouTubeAutoWatchLater(IYouTubeService youTubeService, IConfigurationRepository configurationRepository,
        ILogger<YouTubeAutoWatchLater> logger)
    {
        _youTubeService = youTubeService;
        _configurationRepository = configurationRepository;
        _logger = logger;
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
        _logger.LogInformation("Initializing YouTube service...");
        await _youTubeService.Init();
        _logger.LogInformation("Finished initializing YouTube service");

        _logger.LogInformation("Getting subscriptions...");
        var subscriptions = await _youTubeService.GetMySubscriptions();
        _logger.LogInformation("Finished getting subscriptions");

        _logger.LogInformation("Setting uploads playlist for subscriptions");
        await _youTubeService.SetUploadsPlaylistForSubscriptions(subscriptions);
        _logger.LogInformation("Finished setting uploads playlist of subscriptions");

        _logger.LogInformation("Getting last successful execution date time...");
        var lastSuccessfulExecutionDateTime = await _configurationRepository.GetLastSuccessfulExecutionDateTime();
        _logger.LogInformation(
            $"Finished getting last successful execution date time: {lastSuccessfulExecutionDateTime:o} UTC");

        _logger.LogInformation("Setting recent videos of subscriptions...");
        await _youTubeService.SetRecentVideosForSubscriptions(subscriptions, lastSuccessfulExecutionDateTime);
        _logger.LogInformation("Finished setting recent videos of subscriptions");

        await _youTubeService.AddRecentVideosToPlaylist(subscriptions);

        _logger.LogInformation("Setting last successful execution date time...");
        await _configurationRepository.SetLastSuccessfulExecutionDateTimeToNow();
        _logger.LogInformation("Finished setting last successful execution date time");
    }

    [Singleton]
    [FunctionName(nameof(GetRefreshToken))]
    public async Task<IActionResult> GetRefreshToken([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest request)
    {
        string refreshToken = await _youTubeService.GetRefreshToken();
        return new OkObjectResult(refreshToken);
    }
}
