namespace YouTubeAutoWatchLater.Application.Settings;

public interface ISettings
{
    string RefreshToken { get; }
    string PlaylistId { get; }
}
