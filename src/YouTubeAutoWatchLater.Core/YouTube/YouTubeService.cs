using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.Extensions;
using YouTubeAutoWatchLater.Core.GoogleApis;
using YouTubeAutoWatchLater.Core.Settings;
using YouTubeAutoWatchLater.Core.YouTube.Models;

namespace YouTubeAutoWatchLater.Core.YouTube;

public class YouTubeService : IYouTubeService
{
    private const int MaxResults = 50;

    private readonly IGoogleApis _googleApis;
    private readonly ISettings _settings;
    private readonly ILogger<YouTubeService> _logger;
    private YouTubeApi? _youTubeApi;

    public YouTubeService(IGoogleApis googleApis, ISettings settings, ILogger<YouTubeService> logger)
    {
        _googleApis = googleApis;
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> GetRefreshToken()
    {
        _logger.LogInformation("Starting authorization...");
        var credentials = await _googleApis.Authorize();
        _logger.LogInformation("Authorization finished");

        return credentials.Token.RefreshToken;
    }

    public async Task Init()
    {
        _logger.LogInformation("Getting access token...");
        string accessToken = await _googleApis.GetAccessToken();
        _logger.LogInformation("Finished getting access token");

        _logger.LogInformation("Creating YouTube API...");
        _youTubeApi = _googleApis.CreateYouTubeService(accessToken);
        _logger.LogInformation("Finished creating YouTube API");
    }

    public async Task<Subscriptions> GetMySubscriptions()
    {
        ThrowIfYouTubeServiceIsNull();

        Subscriptions youTubeSubscriptions = new();

        var nextPageToken = string.Empty;
        do
        {
            var subscriptionsListRequest = _youTubeApi!.Subscriptions.List("snippet");
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
        ThrowIfYouTubeServiceIsNull();

        var chunks = subscriptions.Chunk(MaxResults).ToArray();
        for (var i = 0; i < chunks.Length; i++)
        {
            var chunkedSubscriptions = chunks[i];

            _logger.LogInformation($"Getting channels for subscriptions chunk {i + 1} of {chunks.Length}");
            var channelsOfSubscriptions = await _youTubeApi!.GetChannelsOfSubscriptions(chunkedSubscriptions);
            _logger.LogInformation($"Finished getting channels for subscriptions chunk {i + 1} of {chunks.Length}");

            foreach (var channel in channelsOfSubscriptions)
            {
                subscriptions[channel.Id].UploadsPlaylist = channel.ContentDetails.RelatedPlaylists.Uploads;
            }
        }
    }

    public async Task SetRecentVideosForSubscriptions(Subscriptions subscriptions, DateTimeOffset dateTime)
    {
        ThrowIfYouTubeServiceIsNull();

        foreach (var (_, channel) in subscriptions)
        {
            if (string.IsNullOrEmpty(channel.UploadsPlaylist))
                continue;

            _logger.LogInformation($"Getting uploads playlist items of {channel}");
            var recentVideos = await _youTubeApi!.GetRecentVideos(channel.UploadsPlaylist, dateTime);
            _logger.LogInformation($"Finished getting uploads playlist items of {channel}");

            channel.RecentVideos = recentVideos;
        }
    }

    public async Task AddRecentVideosToPlaylist(Subscriptions subscriptions)
    {
        ThrowIfYouTubeServiceIsNull();

        var recentVideos = subscriptions.Values
            .SelectMany(subscription => subscription.RecentVideos!)
            .OrderByDescending(video => video.PublishedAt)
            .ThenByDescending(video => video.AddedToUploadPlaylistAt)
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
            await _youTubeApi!.AddToPlaylist(_settings.PlaylistId, video);
            _logger.LogInformation($"Finished adding video {video} to playlist");
        }

        _logger.LogInformation("Finished adding recent videos to playlist");
    }

    private void ThrowIfYouTubeServiceIsNull()
    {
        if (_youTubeApi is null)
            throw new ApplicationException("YouTube service is not initialized");
    }
}
