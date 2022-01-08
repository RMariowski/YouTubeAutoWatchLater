using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Extensions;
using YouTubeAutoWatchLater.GoogleApis;
using YouTubeAutoWatchLater.Settings;

namespace YouTubeAutoWatchLater;

public class YouTubeAutoWatchLater
{
    public const int MaxResults = 50;

    private readonly IGoogleApis _googleApis;
    private readonly ISettings _settings;
    private readonly ILogger<YouTubeAutoWatchLater> _logger;
    private YouTubeService? _youTubeService;

    public YouTubeAutoWatchLater(IGoogleApis googleApis, ISettings settings, ILogger<YouTubeAutoWatchLater> logger)
    {
        _googleApis = googleApis;
        _settings = settings;
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
        _logger.LogInformation("Getting access token...");
        string accessToken = await _googleApis.GetAccessToken();
        _logger.LogInformation("Finished getting access token");

        _logger.LogInformation("Creating YouTube service...");
        _youTubeService = _googleApis.CreateYouTubeService(accessToken);
        _logger.LogInformation("Finished creating YouTube service");

        _logger.LogInformation("Getting subscriptions...");
        var subscriptions = await _youTubeService.GetMySubscriptions();
        _logger.LogInformation("Finished getting subscriptions");

        _logger.LogInformation("Setting uploads playlist for subscriptions");
        await SetUploadsPlaylistForSubscriptions(subscriptions);
        _logger.LogInformation("Finished setting uploads playlist of subscriptions");

        _logger.LogInformation("Setting recent videos of subscriptions...");
        var newerThan = timerInfo.ScheduleStatus.Last == default ? DateTime.UtcNow : timerInfo.ScheduleStatus.Last;
        await SetRecentVideosForSubscriptions(subscriptions, newerThan);
        _logger.LogInformation("Finished setting recent videos of subscriptions");

        await AddRecentVideosToPlaylist(subscriptions);
    }

    private async Task SetUploadsPlaylistForSubscriptions(Subscriptions subscriptions)
    {
        var chunks = subscriptions.Chunk(MaxResults).ToArray();
        for (var i = 0; i < chunks.Length; i++)
        {
            var chunkedSubscriptions = chunks[i];

            _logger.LogInformation($"Getting channels for subscriptions chunk {i + 1} of {chunks.Length}");
            var channelsOfSubscriptions = await _youTubeService!.GetChannelsOfSubscriptions(chunkedSubscriptions);
            _logger.LogInformation($"Finished getting channels for subscriptions chunk {i + 1} of {chunks.Length}");

            foreach (var channel in channelsOfSubscriptions)
            {
                subscriptions[channel.Id].UploadsPlaylist = channel.ContentDetails.RelatedPlaylists.Uploads;
            }
        }
    }

    private async Task SetRecentVideosForSubscriptions(Subscriptions subscriptions, DateTime dateTime)
    {
        foreach (var (_, channel) in subscriptions)
        {
            if (string.IsNullOrEmpty(channel.UploadsPlaylist))
                continue;

            _logger.LogInformation($"Getting uploads playlist items of {channel}");
            var recentVideos = await _youTubeService!.GetRecentVideos(channel.UploadsPlaylist, dateTime);
            _logger.LogInformation($"Finished getting uploads playlist items of {channel}");

            channel.RecentVideos = recentVideos;
        }
    }

    private async Task AddRecentVideosToPlaylist(Subscriptions subscriptions)
    {
        var recentVideos = subscriptions.Values
            .SelectMany(subscription => subscription.RecentVideos!)
            .OrderByDescending(video => video.PublishedAt)
            .ToArray();

        if (recentVideos.Length == 0)
        {
            _logger.LogInformation("No videos to add");
            return;
        }

        _logger.LogInformation("Adding recent videos to playlist...");

        foreach (var video in recentVideos)
        {
            _logger.LogInformation($"Adding video {video} to playlist");
            await _youTubeService!.AddToPlaylist(_settings.PlaylistId, video);
            _logger.LogInformation($"Finished adding video {video} to playlist");
        }

        _logger.LogInformation("Finished adding recent videos to playlist");
    }
}
