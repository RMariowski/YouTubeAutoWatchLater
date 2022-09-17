using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Application.YouTube.Repositories;

public sealed class YouTubeChannelRepository : IChannelRepository
{
    private readonly YouTubeService _youTubeService;
    private readonly ILogger<YouTubeChannelRepository> _logger;

    public YouTubeChannelRepository(YouTubeService youTubeService, ILogger<YouTubeChannelRepository> logger)
    {
        _youTubeService = youTubeService;
        _logger = logger;
    }

    public async Task SetUploadsPlaylistForSubscriptions(Subscriptions subscriptions)
    {
        var chunks = subscriptions.Chunk(Consts.MaxResults).ToArray();
        for (var i = 0; i < chunks.Length; i++)
        {
            var chunkedSubscriptions = chunks[i];

            _logger.LogInformation($"Getting channels of subscriptions chunk {i + 1}/{chunks.Length}");

            var channelsListRequest = _youTubeService.Channels.List("contentDetails");
            channelsListRequest.Id = chunkedSubscriptions.Select(subscription => subscription.Key.Value).ToArray();
            channelsListRequest.MaxResults = chunkedSubscriptions.Length;
            var channelListResponse = await channelsListRequest.ExecuteAsync();
            var channelsOfSubscriptions = channelListResponse.Items;

            _logger.LogInformation($"Finished getting channels of subscriptions chunk {i + 1}/{chunks.Length}");

            foreach (var channel in channelsOfSubscriptions)
            {
                ChannelId channelId = new(channel.Id);
                PlaylistId playlist = new(channel.ContentDetails.RelatedPlaylists.Uploads);
                subscriptions[channelId].UploadsPlaylist = playlist;
            }
        }
    }
}
