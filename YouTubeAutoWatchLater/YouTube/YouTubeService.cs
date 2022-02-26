using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Extensions;
using YouTubeAutoWatchLater.GoogleApis;
using YouTubeAutoWatchLater.Settings;
using YouTubeAutoWatchLater.YouTube.Models;

namespace YouTubeAutoWatchLater.YouTube;

public class YouTubeService : IYouTubeService
{
    private const int MaxResults = 50;

    private readonly IGoogleApis _googleApis;
    private readonly ISettings _settings;
    private readonly ILogger<YouTubeService> _logger;
    private readonly Lazy<YouTubeApi> _youTubeApi;

    private string _accessToken = string.Empty;

    public YouTubeService(IGoogleApis googleApis, ISettings settings, ILogger<YouTubeService> logger)
    {
        _googleApis = googleApis;
        _settings = settings;
        _logger = logger;
        _youTubeApi = new Lazy<YouTubeApi>(Init);
    }

    public async Task<string> GetRefreshToken()
    {
        _logger.LogInformation("Starting authorization...");
        var credentials = await _googleApis.Authorize();
        _logger.LogInformation("Authorization finished");

        return credentials.Token.RefreshToken;
    }

    public YouTubeApi Init()
    {
        _logger.LogInformation("Initializing YouTube service...");

        if (string.IsNullOrEmpty(_accessToken))
        {
            _logger.LogInformation("Getting access token...");
            _accessToken = _googleApis.GetAccessToken().GetAwaiter().GetResult();
            _logger.LogInformation("Finished getting access token");
        }

        _logger.LogInformation("Creating YouTube API...");
        var youTubeApi = _googleApis.CreateYouTubeService(_accessToken);
        _logger.LogInformation("Finished creating YouTube API");

        _logger.LogInformation("Finished initializing YouTube service");

        return youTubeApi;
    }

    public async Task<Subscriptions> GetMySubscriptions()
    {
        Subscriptions youTubeSubscriptions = new();

        var nextPageToken = string.Empty;
        do
        {
            var subscriptionsListRequest = _youTubeApi.Value.Subscriptions.List("snippet");
            subscriptionsListRequest.MaxResults = MaxResults;
            subscriptionsListRequest.Mine = true;
            subscriptionsListRequest.PageToken = nextPageToken;
            var subscriptionsListResponse = await subscriptionsListRequest.ExecuteAsync();

            var subscriptions = subscriptionsListResponse.Items.Select(YouTubeChannel.From).ToArray();
            foreach (var subscription in subscriptions)
                youTubeSubscriptions.Add(subscription.Id, subscription);

            nextPageToken = subscriptionsListResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(nextPageToken));

        return youTubeSubscriptions;
    }

    public async Task SetUploadsPlaylistForSubscriptions(Subscriptions subscriptions)
    {
        var chunks = subscriptions.Chunk(MaxResults).ToArray();
        for (var i = 0; i < chunks.Length; i++)
        {
            var chunkedSubscriptions = chunks[i];

            _logger.LogInformation($"Getting channels for subscriptions chunk {i + 1} of {chunks.Length}");
            var channelsOfSubscriptions = await _youTubeApi.Value.GetChannelsOfSubscriptions(chunkedSubscriptions);
            _logger.LogInformation($"Finished getting channels for subscriptions chunk {i + 1} of {chunks.Length}");

            foreach (var channel in channelsOfSubscriptions)
            {
                subscriptions[channel.Id].UploadsPlaylist = channel.ContentDetails.RelatedPlaylists.Uploads;
            }
        }
    }

    public async Task<IList<YouTubeVideo>> GetRecentVideosOfChannel(YouTubeChannel channel, DateTimeOffset dateTime)
    {
        if (string.IsNullOrEmpty(channel.UploadsPlaylist))
            return new List<YouTubeVideo>();

        _logger.LogInformation($"Getting uploads playlist items of {channel}");
        var recentVideos = await _youTubeApi.Value.GetRecentVideos(channel.UploadsPlaylist, dateTime);
        _logger.LogInformation($"Finished getting uploads playlist items of {channel}");

        return recentVideos;
    }

    public async Task AddVideosToPlaylist(YouTubeChannel subscription, IList<YouTubeVideo> videos)
    {
        if (videos.Count == 0)
        {
            _logger.LogInformation("No videos to add");
            return;
        }

        var orderedVideos = videos
            .OrderByDescending(video => video.PublishedAt)
            .ThenByDescending(video => video.AddedToUploadPlaylistAt)
            .ToArray();

        _logger.LogInformation("Adding recent videos to playlist...");

        foreach (var video in orderedVideos)
        {
            _logger.LogInformation($"Adding video {video} to playlist");
            await _youTubeApi.Value.AddToPlaylist(_settings.PlaylistId, video);
            _logger.LogInformation($"Finished adding video {video} to playlist");
        }

        _logger.LogInformation("Finished adding recent videos to playlist");
    }
}
