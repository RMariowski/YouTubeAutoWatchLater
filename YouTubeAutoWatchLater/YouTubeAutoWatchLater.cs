using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.YouTube;

namespace YouTubeAutoWatchLater;

public class YouTubeAutoWatchLater
{
    private readonly IYouTubeService _youTubeService;
    private readonly ILogger<YouTubeAutoWatchLater> _logger;

    public YouTubeAutoWatchLater(IYouTubeService youTubeService, ILogger<YouTubeAutoWatchLater> logger)
    {
        _youTubeService = youTubeService;
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

        _logger.LogInformation("Setting recent videos of subscriptions...");
        var newerThan = timerInfo.ScheduleStatus.Last == default ? DateTime.UtcNow : timerInfo.ScheduleStatus.Last;
        await _youTubeService.SetRecentVideosForSubscriptions(subscriptions, newerThan);
        _logger.LogInformation("Finished setting recent videos of subscriptions");

        await _youTubeService.AddRecentVideosToPlaylist(subscriptions);
    }
}
