using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IAutoAddedVideosRepository
{
    Task Add(ChannelId channelId, Video video);

    Task<IReadOnlyList<VideoId>> GetAutoAddedVideos(ChannelId channelId);

    Task DeleteOlderThan(DateTimeOffset dateTime);
}
