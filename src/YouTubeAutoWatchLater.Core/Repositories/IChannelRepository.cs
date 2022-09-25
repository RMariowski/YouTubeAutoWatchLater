using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IChannelRepository
{
    Task<IReadOnlyDictionary<ChannelId, PlaylistId>> GetUploadsPlaylists(IEnumerable<ChannelId> channelIds);
}
