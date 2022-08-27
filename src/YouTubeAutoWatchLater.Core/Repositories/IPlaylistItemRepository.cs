using YouTubeAutoWatchLater.Core.Models;
using Video = YouTubeAutoWatchLater.Core.Models.Video;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IPlaylistItemRepository
{
    Task AddToPlaylist(string playlistId, Video video);

    Task<IReadOnlyList<Video>> GetRecentVideos(string playlistId, DateTimeOffset dateTime);
    
    Task<IReadOnlyList<PlaylistItem>> GetPrivatePlaylistItemsOfPlaylist(string playlistId);
    
    Task DeletePlaylistItem(PlaylistItemId id);

    Task DeletePrivatePlaylistItemsOfPlaylist(string playlistId);
}
