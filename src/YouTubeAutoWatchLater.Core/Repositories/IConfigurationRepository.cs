namespace YouTubeAutoWatchLater.Core.Repositories;

public interface IConfigurationRepository
{
    Task<DateTime> GetLastSuccessfulExecutionDateTime();

    Task SetLastSuccessfulExecutionDateTimeToNow();
}
