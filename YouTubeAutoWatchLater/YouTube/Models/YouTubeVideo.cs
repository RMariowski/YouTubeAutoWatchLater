namespace YouTubeAutoWatchLater.YouTube.Models;

public record YouTubeVideo(string Id, string Kind, string Title, DateTime PublishedAt)
{
    public static YouTubeVideo From(PlaylistItem playlistItem)
    {
        var snippet = playlistItem.Snippet;
        YouTubeVideo youTubeVideo = new
        (
            snippet.ResourceId.VideoId,
            snippet.ResourceId.Kind,
            snippet.Title,
            snippet.PublishedAt!.Value
        );
        return youTubeVideo;
    }
}
