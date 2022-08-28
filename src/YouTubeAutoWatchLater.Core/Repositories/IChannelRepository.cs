using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IChannelRepository
{
    Task SetUploadsPlaylistForSubscriptions(Subscriptions subscriptions);
}
