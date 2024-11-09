using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;
using YouTubePlaylistItem = Google.Apis.YouTube.v3.Data.PlaylistItem;
using PlaylistItem = YouTubeAutoWatchLater.Core.Models.PlaylistItem;
using Video = YouTubeAutoWatchLater.Core.Models.Video;

namespace YouTubeAutoWatchLater.Core.YouTube.Repositories;

internal sealed class YouTubePlaylistItemRepository : IPlaylistItemRepository
{
    private readonly YouTubeService _youTubeService;
    private readonly ILogger<YouTubePlaylistItemRepository> _logger;

    public YouTubePlaylistItemRepository(YouTubeService youTubeService, ILogger<YouTubePlaylistItemRepository> logger)
    {
        _youTubeService = youTubeService;
        _logger = logger;
    }

    public async Task AddToPlaylistAsync(PlaylistId playlistId, Video video)
    {
        YouTubePlaylistItem playlistItem = new()
        {
            Snippet = new PlaylistItemSnippet
            {
                Position = 0,
                PlaylistId = playlistId.Value,
                ResourceId = new ResourceId
                {
                    VideoId = video.Id.Value,
                    Kind = video.Kind
                }
            }
        };

        await _youTubeService.PlaylistItems.Insert(playlistItem, "snippet").ExecuteAsync();
    }

    public async Task<IReadOnlyList<Video>> GetVideosAsync(PlaylistId playlistId, DateTimeOffset since)
    {
        List<Video> recentVideos = [];

        var pageToken = string.Empty;
        do
        {
            var playlistItemsListRequest = _youTubeService.PlaylistItems.List("snippet,contentDetails");
            playlistItemsListRequest.PlaylistId = playlistId.Value;
            playlistItemsListRequest.MaxResults = Consts.MaxResults;
            playlistItemsListRequest.PageToken = pageToken;
            var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

            var videosNewerThanSpecifiedDateTime = playlistItemsListResponse.Items
                .Where(playlistItem => playlistItem.ContentDetails.VideoPublishedAtDateTimeOffset > since ||
                                       playlistItem.Snippet.PublishedAtDateTimeOffset > since)
                .Select(playlistItem => new Video
                (
                    new VideoId(playlistItem.Snippet.ResourceId.VideoId),
                    playlistItem.Snippet.ResourceId.Kind,
                    playlistItem.Snippet.Title,
                    playlistItem.Snippet.ChannelTitle,
                    playlistItem.ContentDetails.VideoPublishedAtDateTimeOffset!.Value,
                    playlistItem.Snippet.PublishedAtDateTimeOffset!.Value
                ))
                .ToArray();

            recentVideos.AddRange(videosNewerThanSpecifiedDateTime);

            if (videosNewerThanSpecifiedDateTime.Length == 0)
                break;

            pageToken = playlistItemsListResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return recentVideos;
    }

    public async Task<IReadOnlyList<PlaylistItem>> GetPrivatePlaylistItemsOfPlaylistAsync(PlaylistId playlistId)
    {
        _logger.LogInformation("Getting private playlist items from playlist {PlaylistId}", playlistId);

        List<PlaylistItem> playlistItems = [];

        var pageToken = string.Empty;
        do
        {
            var playlistItemsListRequest = _youTubeService.PlaylistItems.List("id,status");
            playlistItemsListRequest.PlaylistId = playlistId.Value;
            playlistItemsListRequest.MaxResults = Consts.MaxResults;
            playlistItemsListRequest.PageToken = pageToken;
            var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

            var privatePlaylistItems = playlistItemsListResponse.Items
                .Where(item => item.Status.PrivacyStatus is "private" or "privacyStatusUnspecified")
                .Select(item => new PlaylistItem(new PlaylistItemId(item.Id)));
            playlistItems.AddRange(privatePlaylistItems);

            pageToken = playlistItemsListResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        _logger.LogInformation("Finished getting private playlist items from playlist {PlaylistId}", playlistId);

        return playlistItems;
    }

    public async Task DeletePlaylistItemAsync(PlaylistItemId playlistId)
    {
        _logger.LogInformation("Deleting playlist item {PlaylistId}", playlistId);
        var playlistItemsDeleteRequest = _youTubeService.PlaylistItems.Delete(playlistId.Value);
        _ = await playlistItemsDeleteRequest.ExecuteAsync();
        _logger.LogInformation("Finished deleting playlist item {PlaylistId}", playlistId);
    }
}
