using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Core.YouTube.Repositories;

internal sealed class YouTubeChannelRepository : IChannelRepository
{
    private readonly YouTubeService _youTubeService;
    private readonly ILogger<YouTubeChannelRepository> _logger;

    public YouTubeChannelRepository(YouTubeService youTubeService, ILogger<YouTubeChannelRepository> logger)
    {
        _youTubeService = youTubeService;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<ChannelId, PlaylistId>> GetUploadsPlaylistsAsync(
        IEnumerable<ChannelId> channelIds)
    {
        Dictionary<ChannelId, PlaylistId> result = new();

        var chunks = channelIds.Chunk(Consts.MaxResults).ToArray();
        for (var i = 0; i < chunks.Length; i++)
        {
            var chunkedChannelIds = chunks[i];

            _logger.LogInformation("Getting channels of subscriptions chunk {CurrentChunk}/{Chunks}", 
                i + 1, chunks.Length);

            var channelsListRequest = _youTubeService.Channels.List("contentDetails");
            channelsListRequest.Id = chunkedChannelIds.Select(id => id.Value).ToArray();
            channelsListRequest.MaxResults = chunkedChannelIds.Length;
            var channelListResponse = await channelsListRequest.ExecuteAsync();

            _logger.LogInformation("Finished getting channels of subscriptions chunk {CurrentChunk}/{Chunks}", 
                i + 1, chunks.Length);

            foreach (var channel in channelListResponse.Items)
            {
                ChannelId channelId = new(channel.Id);
                PlaylistId playlist = new(channel.ContentDetails.RelatedPlaylists.Uploads);
                result.Add(channelId, playlist);
            }
        }

        return result;
    }
}
