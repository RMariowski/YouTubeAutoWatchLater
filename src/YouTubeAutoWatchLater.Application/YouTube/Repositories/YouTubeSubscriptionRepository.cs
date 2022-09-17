using Google.Apis.YouTube.v3;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Application.YouTube.Repositories;

public sealed class YouTubeSubscriptionRepository : ISubscriptionRepository
{
    private readonly YouTubeService _youTubeService;

    public YouTubeSubscriptionRepository(YouTubeService youTubeService)
    {
        _youTubeService = youTubeService;
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
                    new ChannelId(subscription.Snippet.ResourceId.ChannelId),
                    subscription.Snippet.Title
                ))
                .ToArray();
            foreach (var subscription in subscriptions)
                youTubeSubscriptions.Add(subscription.Id, subscription);

            nextPageToken = subscriptionsListResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(nextPageToken));

        return youTubeSubscriptions;
    }
}
