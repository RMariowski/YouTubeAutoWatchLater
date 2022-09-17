namespace YouTubeAutoWatchLater.Core.Models;

public sealed  record Video
(
    VideoId Id,
    string Kind,
    string Title,
    DateTime PublishedAt,
    DateTime AddedToUploadPlaylistAt
);
