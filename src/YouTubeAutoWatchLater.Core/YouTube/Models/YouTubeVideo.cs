namespace YouTubeAutoWatchLater.Core.YouTube.Models;

public record YouTubeVideo
(
    string Id,
    string Kind,
    string Title,
    DateTime PublishedAt,
    DateTime AddedToUploadPlaylistAt
)
{
    public static YouTubeVideo From(PlaylistItem playlistItem)
    {
        var snippet = playlistItem.Snippet;
        ArgumentNullException.ThrowIfNull(snippet);

        var contentDetails = playlistItem.ContentDetails;
        ArgumentNullException.ThrowIfNull(contentDetails);

        YouTubeVideo youTubeVideo = new
        (
            snippet.ResourceId.VideoId,
            snippet.ResourceId.Kind,
            snippet.Title,
            contentDetails.VideoPublishedAt!.Value,
            snippet.PublishedAt!.Value
        );
        return youTubeVideo;
    }
}
