﻿namespace YouTubeAutoWatchLater.Core.Models;

public record Channel(ChannelId Id, string Name)
{
    public PlaylistId? UploadsPlaylist { get; set; }
    public IReadOnlyList<Video>? RecentVideos { get; set; }

    public override string ToString()
        => $"Channel {{ Id = {Id.Value}, Name = {Name} }}";
}
