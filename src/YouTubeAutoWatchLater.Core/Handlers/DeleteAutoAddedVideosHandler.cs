using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Core.Handlers;

public interface IDeleteAutoAddedVideosHandler
{
    Task HandleAsync();
}

internal sealed class DeleteAutoAddedVideosHandler : IDeleteAutoAddedVideosHandler
{
    private readonly IAutoAddedVideosRepository _autoAddedVideosRepository;
    private readonly ILogger<DeleteAutoAddedVideosHandler> _logger;

    public DeleteAutoAddedVideosHandler(IAutoAddedVideosRepository autoAddedVideosRepository,
        ILogger<DeleteAutoAddedVideosHandler> logger)
    {
        _autoAddedVideosRepository = autoAddedVideosRepository;
        _logger = logger;
    }

    public async Task HandleAsync()
    {
        var olderThanDaysString = Environment.GetEnvironmentVariable("DeleteAutoAddedVideos:OlderThanDays");
        if (string.IsNullOrWhiteSpace(olderThanDaysString))
            return;

        if (int.TryParse(olderThanDaysString, out var olderThanDays) is false)
            return;

        var dateTime = DateTimeOffset.UtcNow.AddDays(-olderThanDays);
        var formattedDateTime = dateTime.ToString("yyyy-MM-ddTHH:mm");

        _logger.LogInformation("Deleting auto added videos older than {DateTime}", formattedDateTime);
        await _autoAddedVideosRepository.DeleteOlderThanAsync(dateTime);
        _logger.LogInformation("Finished deleting auto added videos older than {DateTime}", formattedDateTime);
    }
}
