﻿using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Application.Repositories;

public interface IAutoAddedVideosRepository
{
    Task Add(ChannelId channelId, Video[] videos);

    Task<IReadOnlyList<VideoId>> GetAutoAddedVideos(ChannelId channelId);

    Task DeleteOlderThan(DateTimeOffset dateTime);
}
