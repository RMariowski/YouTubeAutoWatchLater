namespace YouTubeAutoWatchLater.Core.Models;

public record Channel(string Id, string Name)
{
    public string? UploadsPlaylist { get; set; }
    public IReadOnlyList<Video>? RecentVideos { get; set; }

    public override string ToString()
        => $"Channel {{ Id = {Id}, Name = {Name} }}";
}
