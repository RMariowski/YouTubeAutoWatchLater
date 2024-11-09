using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IPlaylistItemRepository
{
    Task AddToPlaylistAsync(PlaylistId playlistId, Video video);

    Task<IReadOnlyList<Video>> GetVideosAsync(PlaylistId playlistId, DateTimeOffset since);

    Task<IReadOnlyList<PlaylistItem>> GetPrivatePlaylistItemsOfPlaylistAsync(PlaylistId playlistId);

    Task DeletePlaylistItemAsync(PlaylistItemId playlistId);
}
