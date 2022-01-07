namespace YouTubeAutoWatchLater;

public class Settings : ISettings
{
    public string GetRefreshToken()
    {
        const string refreshTokenSettingKey = "YouTube:RefreshToken";
        string? refreshToken = Environment.GetEnvironmentVariable(refreshTokenSettingKey);
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ApplicationException($"Missing setting value of {refreshTokenSettingKey}");
        return refreshToken;
    }
}