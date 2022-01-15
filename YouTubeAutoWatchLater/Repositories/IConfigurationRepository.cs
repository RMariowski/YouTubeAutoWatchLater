namespace YouTubeAutoWatchLater.Repositories;

public interface IConfigurationRepository
{
    Task<DateTime> GetLastSuccessfulExecutionDateTime();

    Task SetLastSuccessfulExecutionDateTimeToNow();
}
