namespace YouTubeAutoWatchLater.Repositories.Configuration;

public interface IConfigurationRepository
{
    Task<DateTime> GetLastSuccessfulExecutionDateTime();

    Task SetLastSuccessfulExecutionDateTimeToNow();
}
