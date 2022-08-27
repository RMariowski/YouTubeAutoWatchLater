using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IChannelRepository
{
    Task<Subscriptions> GetMySubscriptions();

    Task SetUploadsPlaylistForSubscriptions(Subscriptions subscriptions);
}
