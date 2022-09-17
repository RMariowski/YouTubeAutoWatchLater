using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Application.YouTube.Services;

public interface IPlaylistRuleResolver
{
    PlaylistId Resolve(Video video);
}
