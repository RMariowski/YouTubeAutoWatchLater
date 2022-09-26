namespace YouTubeAutoWatchLater.Core.Models;

public sealed record Channel(ChannelId Id, string Name)
{
    public PlaylistId? UploadsPlaylist { get; set; }

    public override string ToString()
        => $"Channel {{ Id = {Id.Value}, Name = {Name} }}";
}
