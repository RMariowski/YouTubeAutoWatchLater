using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscriptions> GetMySubscriptions();
}