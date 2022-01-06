using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace YouTubeAutoWatchLater;

public class YouTubeAutoWatchLater
{
    public const int MaxResults = 50;

    private ILogger? _logger;
    private YouTubeService? _youTubeService;

    [Singleton]
    [FunctionName(nameof(Run))]
    public async Task Run([TimerTrigger("%Cron%", RunOnStartup = true)] TimerInfo timerInfo, ILogger log)
    {
        _logger = log;

        log.LogInformation("Getting access token...");
        string accessToken = await GoogleApis.GetAccessToken();
        log.LogInformation("Finished getting access token");

        log.LogInformation("Creating YouTube service...");
        _youTubeService = GoogleApis.CreateYouTubeService(accessToken);
        log.LogInformation("Finished creating YouTube service");

        log.LogInformation("Getting subscriptions...");
        var subscriptions = await _youTubeService.GetMySubscriptions();
        log.LogInformation("Finished getting subscriptions");

        log.LogInformation("Setting uploads playlist for subscriptions");
        await SetUploadsPlaylistForSubscriptions(subscriptions);
        log.LogInformation("Finished setting uploads playlist of subscriptions");

        log.LogInformation("Setting recent videos of subscriptions...");
        var newerThan = timerInfo.ScheduleStatus.Last == default ? DateTime.UtcNow : timerInfo.ScheduleStatus.Last;
        await SetRecentVideosForSubscriptions(subscriptions, newerThan);
        log.LogInformation("Finished setting recent videos of subscriptions");

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
        string playlistId = Environment.GetEnvironmentVariable("YouTube:PlaylistId")!;

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
            await _youTubeService!.AddToPlaylist(playlistId, video);
            _logger.LogInformation($"Finished adding video {video} to playlist");
        }

        _logger.LogInformation("Finished adding recent videos to playlist");
    }
}
