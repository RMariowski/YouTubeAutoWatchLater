namespace YouTubeAutoWatchLater.Application.Repositories;

public interface IConfigurationRepository
{
    Task<DateTime> GetLastSuccessfulExecutionDateTime();

    Task SetLastSuccessfulExecutionDateTimeToNow();
}
