namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IConfigurationRepository
{
    Task<DateTime> GetLastSuccessfulExecutionDateTimeAsync();

    Task SetLastSuccessfulExecutionDateTimeToNowAsync();
}
