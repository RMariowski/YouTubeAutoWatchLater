using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IChannelRepository
{
    Task<IReadOnlyDictionary<ChannelId, PlaylistId>> GetUploadsPlaylistsAsync(IEnumerable<ChannelId> channelIds);
}
