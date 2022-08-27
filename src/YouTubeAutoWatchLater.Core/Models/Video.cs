namespace YouTubeAutoWatchLater.Core.Models;

public record Video
(
    VideoId Id,
    string Kind,
    string Title,
    DateTime PublishedAt,
    DateTime AddedToUploadPlaylistAt
);
