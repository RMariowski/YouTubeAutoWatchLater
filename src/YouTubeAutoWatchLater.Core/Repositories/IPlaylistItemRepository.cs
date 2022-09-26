using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IPlaylistItemRepository
{
    Task AddToPlaylist(PlaylistId playlistId, Video video);

    Task<IReadOnlyList<Video>> GetVideos(PlaylistId playlistId, DateTimeOffset since);

    Task<IReadOnlyList<PlaylistItem>> GetPrivatePlaylistItemsOfPlaylist(PlaylistId playlistId);

    Task DeletePlaylistItem(PlaylistItemId id);
}
