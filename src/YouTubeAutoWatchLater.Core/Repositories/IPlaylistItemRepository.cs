using YouTubeAutoWatchLater.Core.Models;
using Video = YouTubeAutoWatchLater.Core.Models.Video;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IPlaylistItemRepository
{
    Task AddToPlaylist(PlaylistId playlistId, Video video);

    Task<IReadOnlyList<Video>> GetRecentVideos(PlaylistId playlistId, DateTimeOffset dateTime);
    
    Task<IReadOnlyList<PlaylistItem>> GetPrivatePlaylistItemsOfPlaylist(PlaylistId playlistId);
    
    Task DeletePlaylistItem(PlaylistItemId id);

    Task DeletePrivatePlaylistItemsOfPlaylist(PlaylistId playlistId);
}
