using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;
using YouTubeAutoWatchLater.Core.YouTube.Services;

namespace YouTubeAutoWatchLater.Core.YouTube.Repositories;

internal sealed class YouTubeSubscriptionRepository : ISubscriptionRepository
{
    private readonly IYouTubeApi _youTubeApi;

    public YouTubeSubscriptionRepository(IYouTubeApi youTubeApi)
    {
        _youTubeApi = youTubeApi;
    }

    public async Task<Subscriptions> GetMySubscriptionsAsync()
    {
        Subscriptions youTubeSubscriptions = new();

        var pageToken = string.Empty;
        do
        {
            var subscriptionsListResponse = await _youTubeApi.GetSubscriptionsAsync(pageToken);

            var subscriptions = subscriptionsListResponse.Items
                .Select(subscription => new Channel
                (
                    new ChannelId(subscription.Snippet.ResourceId.ChannelId),
                    subscription.Snippet.Title
                ))
                .ToArray();

            foreach (var subscription in subscriptions)
                _ = youTubeSubscriptions.TryAdd(subscription.Id, subscription);

            pageToken = subscriptionsListResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return youTubeSubscriptions;
    }
}
