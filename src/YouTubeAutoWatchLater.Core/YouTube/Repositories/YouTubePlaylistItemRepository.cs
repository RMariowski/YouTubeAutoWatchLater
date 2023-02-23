using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core;
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

    public async Task AddToPlaylist(PlaylistId playlistId, Video video)
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

    public async Task<IReadOnlyList<Video>> GetVideos(PlaylistId playlistId, DateTimeOffset since)
    {
        List<Video> recentVideos = new();

        var pageToken = string.Empty;
        do
        {
            var playlistItemsListRequest = _youTubeService.PlaylistItems.List("snippet,contentDetails");
            playlistItemsListRequest.PlaylistId = playlistId.Value;
            playlistItemsListRequest.MaxResults = Consts.MaxResults;
            playlistItemsListRequest.PageToken = pageToken;
            var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

            var videosNewerThanSpecifiedDateTime = playlistItemsListResponse.Items
                .Where(playlistItem => playlistItem.ContentDetails.VideoPublishedAt > since ||
                                       playlistItem.Snippet.PublishedAt > since)
                .Select(playlistItem => new Video
                (
                    new VideoId(playlistItem.Snippet.ResourceId.VideoId),
                    playlistItem.Snippet.ResourceId.Kind,
                    playlistItem.Snippet.Title,
                    playlistItem.Snippet.ChannelTitle,
                    playlistItem.ContentDetails.VideoPublishedAt!.Value,
                    playlistItem.Snippet.PublishedAt!.Value
                ))
                .ToArray();

            recentVideos.AddRange(videosNewerThanSpecifiedDateTime);

            if (videosNewerThanSpecifiedDateTime.Length == 0)
                break;

            pageToken = playlistItemsListResponse.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return recentVideos;
    }

    public async Task<IReadOnlyList<PlaylistItem>> GetPrivatePlaylistItemsOfPlaylist(PlaylistId playlistId)
    {
        _logger.LogInformation($"Getting private playlist items from playlist {playlistId}");

        List<PlaylistItem> playlistItems = new();

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

        _logger.LogInformation($"Finished getting private playlist items from playlist {playlistId}");

        return playlistItems;
    }

    public async Task DeletePlaylistItem(PlaylistItemId id)
    {
        _logger.LogInformation($"Deleting playlist item {id}");
        var playlistItemsDeleteRequest = _youTubeService.PlaylistItems.Delete(id.Value);
        _ = await playlistItemsDeleteRequest.ExecuteAsync();
        _logger.LogInformation($"Finished deleting playlist item {id}");
    }
}
