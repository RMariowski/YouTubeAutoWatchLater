using MediatR;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Application.Repositories;

namespace YouTubeAutoWatchLater.Application.Handlers;

public class DeleteAutoAddedVideos
{
    public sealed record Command : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IAutoAddedVideosRepository _autoAddedVideosRepository;
        private readonly ILogger<Handler> _logger;

        public Handler(IAutoAddedVideosRepository autoAddedVideosRepository, ILogger<Handler> logger)
        {
            _autoAddedVideosRepository = autoAddedVideosRepository;
            _logger = logger;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var olderThanDaysString = Environment.GetEnvironmentVariable("DeleteAutoAddedVideos:OlderThanDays");
            if (string.IsNullOrWhiteSpace(olderThanDaysString))
                return;

            if (int.TryParse(olderThanDaysString, out var olderThanDays) is false)
                return;

            var dateTime = DateTimeOffset.UtcNow.AddDays(-olderThanDays);

            _logger.LogInformation($"Deleting auto added videos older than {dateTime:yyyy-MM-ddTHH:mm}");
            await _autoAddedVideosRepository.DeleteOlderThan(dateTime);
            _logger.LogInformation($"Finished deleting auto added videos older than {dateTime:yyyy-MM-ddTHH:mm}");
        }
    }
}
