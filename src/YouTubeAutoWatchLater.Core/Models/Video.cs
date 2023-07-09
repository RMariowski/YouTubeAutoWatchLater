namespace YouTubeAutoWatchLater.Core.Models;

public sealed record Video
(
    VideoId Id,
    string Kind,
    string Title,
    string ChannelTitle,
    DateTimeOffset PublishedAt,
    DateTimeOffset AddedToUploadPlaylistAt
);
