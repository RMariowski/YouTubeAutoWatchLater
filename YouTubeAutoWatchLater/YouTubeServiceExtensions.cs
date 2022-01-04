namespace YouTubeAutoWatchLater;

public static class YouTubeServiceExtensions
{
    public static async Task<Dictionary<string, YouTubeChannel>> GetMySubscriptions(
        this YouTubeService youTubeService)
    {
        Dictionary<string, YouTubeChannel> youTubeSubscriptions = new();

        var nextPageToken = string.Empty;
        do
        {
            var subscriptionsListRequest = youTubeService.Subscriptions.List("snippet");
            subscriptionsListRequest.MaxResults = YouTubeAutoWatchLater.MaxResults;
            subscriptionsListRequest.Mine = true;
            subscriptionsListRequest.PageToken = nextPageToken;
            var subscriptionsListResponse = await subscriptionsListRequest.ExecuteAsync();

            var subscriptions = subscriptionsListResponse.Items.Select(YouTubeChannel.From).ToArray();
            foreach (var subscription in subscriptions)
                youTubeSubscriptions.Add(subscription.Id, subscription);

            nextPageToken = subscriptionsListResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(nextPageToken));

        return youTubeSubscriptions;
    }

    public static async Task<IList<Channel>> GetChannelsOfSubscriptions(this YouTubeService youTubeService,
        KeyValuePair<string, YouTubeChannel>[] subscriptions)
    {
        var channelsListRequest = youTubeService.Channels.List("contentDetails");
        channelsListRequest.Id = subscriptions.Select(subscription => subscription.Key).ToArray();
        channelsListRequest.MaxResults = subscriptions.Length;
        var channelListResponse = await channelsListRequest.ExecuteAsync();
        return channelListResponse.Items;
    }

    public static async Task<IList<YouTubeVideo>> GetRecentVideos(this YouTubeService youTubeService,
        string playlistId, DateTime dateTime)
    {
        const int fetchCount = 5;

        List<YouTubeVideo> recentVideos = new();

        string nextPageToken;
        do
        {
            var playlistItemsListRequest = youTubeService.PlaylistItems.List("snippet");
            playlistItemsListRequest.PlaylistId = playlistId;
            playlistItemsListRequest.MaxResults = fetchCount;
            var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

            var videosNewerThanSpecifiedDateTime = playlistItemsListResponse.Items
                .Where(playlistItem => playlistItem.Snippet.PublishedAt > dateTime)
                .Select(YouTubeVideo.From)
                .ToArray();

            if (videosNewerThanSpecifiedDateTime.Length > 0)
                recentVideos.AddRange(videosNewerThanSpecifiedDateTime);

            if (videosNewerThanSpecifiedDateTime.Length < fetchCount)
                break;

            nextPageToken = playlistItemsListResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(nextPageToken));

        return recentVideos;
    }

    public static async Task AddToPlaylist(this YouTubeService youTubeService, string playlistId, YouTubeVideo video)
    {
        var playlistItem = new PlaylistItem
        {
            Snippet = new PlaylistItemSnippet
            {
                Position = 0,
                PlaylistId = playlistId,
                ResourceId = new ResourceId
                {
                    VideoId = video.Id,
                    Kind = video.Kind
                }
            }
        };
        var playlistItemsInsertRequest = youTubeService.PlaylistItems.Insert(playlistItem, "snippet");
        await playlistItemsInsertRequest.ExecuteAsync();
    }
}
