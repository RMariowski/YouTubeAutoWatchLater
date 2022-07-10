namespace YouTubeAutoWatchLater.Core.YouTube.Models;

public record YouTubeChannel(string Id, string Name)
{
    public string? UploadsPlaylist { get; set; }
    public IList<YouTubeVideo>? RecentVideos { get; set; }

    public static YouTubeChannel From(Subscription subscription)
    {
        var snippet = subscription.Snippet;
        YouTubeChannel youTubeSubscription = new(snippet.ResourceId.ChannelId, snippet.Title);
        return youTubeSubscription;
    }

    public override string ToString()
        => $"Channel {{ Id = {Id}, Name = {Name} }}";
}
