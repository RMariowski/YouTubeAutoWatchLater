namespace YouTubeAutoWatchLater;

public class Settings : ISettings
{
    public string RefreshToken { get; }
    public string PlaylistId { get; }

    public Settings()
    {
        RefreshToken = GetRefreshToken();
        PlaylistId = GetPlaylistId();
    }

    private static string GetRefreshToken()
    {
        const string settingKey = "YouTube:RefreshToken";
        string? refreshToken = Environment.GetEnvironmentVariable(settingKey);
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ApplicationException($"Missing setting value of {settingKey}");
        return refreshToken;
    }

    private static string GetPlaylistId()
    {
        const string settingKey = "YouTube:PlaylistId";
        string? playlistId = Environment.GetEnvironmentVariable(settingKey);
        if (string.IsNullOrWhiteSpace(playlistId))
            throw new ApplicationException($"Missing setting value of {settingKey}");
        return playlistId;
    }
}
