using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;
using YouTubeAutoWatchLater.Core.YouTube.Services;

namespace YouTubeAutoWatchLater.Core.YouTube.Repositories;

internal sealed class YouTubeChannelRepository : IChannelRepository
{
    private readonly IYouTubeApi _youTubeApi;
    private readonly ILogger<YouTubeChannelRepository> _logger;

    public YouTubeChannelRepository(IYouTubeApi youTubeApi, ILogger<YouTubeChannelRepository> logger)
    {
        _youTubeApi = youTubeApi;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<ChannelId, PlaylistId>> GetUploadsPlaylistsAsync(
        IEnumerable<ChannelId> channelIds)
    {
        Dictionary<ChannelId, PlaylistId> result = new();

        var chunks = channelIds.Chunk(Consts.MaxResults).ToArray();
        for (var i = 0; i < chunks.Length; i++)
        {
            _logger.LogInformation("Getting channels of subscriptions chunk {CurrentChunk}/{Chunks}", 
                i + 1, chunks.Length);

            var channelListResponse = await _youTubeApi.GetChannelsAsync(chunks[i]);

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
