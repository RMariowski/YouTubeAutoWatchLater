using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IAutoAddedVideosRepository
{
    Task AddAsync(ChannelId channelId, Video video);

    Task<IReadOnlyList<VideoId>> GetAutoAddedVideosAsync(ChannelId channelId);

    Task DeleteOlderThanAsync(DateTimeOffset dateTime);
}
