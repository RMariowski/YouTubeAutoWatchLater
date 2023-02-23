using YouTubeAutoWatchLater.Core.Models;

namespace YouTubeAutoWatchLater.Core.YouTube.Services;

public interface IPlaylistRuleResolver
{
    PlaylistId Resolve(Video video);
}
