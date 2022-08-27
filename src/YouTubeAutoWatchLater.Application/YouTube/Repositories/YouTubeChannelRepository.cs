using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Application.YouTube.Repositories;

public class YouTubeChannelRepository : IChannelRepository
{
    private readonly YouTubeService _youTubeService;
    private readonly ILogger<YouTubeChannelRepository> _logger;

    public YouTubeChannelRepository(YouTubeService youTubeService, ILogger<YouTubeChannelRepository> logger)
    {
        _youTubeService = youTubeService;
        _logger = logger;
    }

    public async Task<Subscriptions> GetMySubscriptions()
    {
        Subscriptions youTubeSubscriptions = new();

        var nextPageToken = string.Empty;
        do
        {
            var subscriptionsListRequest = _youTubeService.Subscriptions.List("snippet");
            subscriptionsListRequest.MaxResults = Consts.MaxResults;
            subscriptionsListRequest.Mine = true;
            subscriptionsListRequest.PageToken = nextPageToken;
            var subscriptionsListResponse = await subscriptionsListRequest.ExecuteAsync();

            var subscriptions = subscriptionsListResponse.Items
                .Select(subscription => new Channel
                (
                    subscription.Snippet.ResourceId.ChannelId,
                    subscription.Snippet.Title
                ))
                .ToArray();
            foreach (var subscription in subscriptions)
                youTubeSubscriptions.Add(subscription.Id, subscription);

            nextPageToken = subscriptionsListResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(nextPageToken));

        return youTubeSubscriptions;
    }

    public async Task SetUploadsPlaylistForSubscriptions(Subscriptions subscriptions)
    {
        var chunks = subscriptions.Chunk(Consts.MaxResults).ToArray();
        for (var i = 0; i < chunks.Length; i++)
        {
            var chunkedSubscriptions = chunks[i];

            _logger.LogInformation($"Getting channels of subscriptions chunk {i + 1}/{chunks.Length}");

            var channelsListRequest = _youTubeService.Channels.List("contentDetails");
            channelsListRequest.Id = chunkedSubscriptions.Select(subscription => subscription.Key).ToArray();
            channelsListRequest.MaxResults = chunkedSubscriptions.Length;
            var channelListResponse = await channelsListRequest.ExecuteAsync();
            var channelsOfSubscriptions = channelListResponse.Items;

            _logger.LogInformation($"Finished getting channels of subscriptions chunk {i + 1}/{chunks.Length}");

            foreach (var channel in channelsOfSubscriptions)
            {
                subscriptions[channel.Id].UploadsPlaylist = channel.ContentDetails.RelatedPlaylists.Uploads;
            }
        }
    }
}
