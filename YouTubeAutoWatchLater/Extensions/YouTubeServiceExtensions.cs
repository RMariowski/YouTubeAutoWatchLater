using YouTubeAutoWatchLater.YouTube.Models;

namespace YouTubeAutoWatchLater.Extensions;

public static class YouTubeServiceExtensions
{
    public static async Task<IList<Channel>> GetChannelsOfSubscriptions(this YouTubeApi youTubeApi,
        KeyValuePair<string, YouTubeChannel>[] subscriptions)
    {
        var channelsListRequest = youTubeApi.Channels.List("contentDetails");
        channelsListRequest.Id = subscriptions.Select(subscription => subscription.Key).ToArray();
        channelsListRequest.MaxResults = subscriptions.Length;
        var channelListResponse = await channelsListRequest.ExecuteAsync();
        return channelListResponse.Items;
    }

    public static async Task<IList<YouTubeVideo>> GetRecentVideos(this YouTubeApi youTubeApi,
        string playlistId, DateTimeOffset dateTime)
    {
        const int fetchCount = 10;

        List<YouTubeVideo> recentVideos = new();

        string nextPageToken;
        do
        {
            var playlistItemsListRequest = youTubeApi.PlaylistItems.List("snippet,contentDetails");
            playlistItemsListRequest.PlaylistId = playlistId;
            playlistItemsListRequest.MaxResults = fetchCount;
            var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

            var videosNewerThanSpecifiedDateTime = playlistItemsListResponse.Items
                .Where(playlistItem => playlistItem.ContentDetails.VideoPublishedAt > dateTime)
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

    public static async Task AddToPlaylist(this YouTubeApi youTubeApi, string playlistId, YouTubeVideo video)
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
        var playlistItemsInsertRequest = youTubeApi.PlaylistItems.Insert(playlistItem, "snippet");
        await playlistItemsInsertRequest.ExecuteAsync();
    }
}
