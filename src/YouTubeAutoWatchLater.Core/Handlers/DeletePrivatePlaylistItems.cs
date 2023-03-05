using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YouTubeAutoWatchLater.Core.YouTube;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Core.Handlers;

public sealed class DeletePrivatePlaylistItems
{
    public sealed record Command : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IPlaylistItemRepository _playlistItemRepository;
        private readonly YouTubeOptions _options;
        private readonly ILogger<Handler> _logger;

        public Handler(IPlaylistItemRepository playlistItemRepository,
            IOptions<YouTubeOptions> options, ILogger<Handler> logger)
        {
            _playlistItemRepository = playlistItemRepository;
            _options = options.Value;
            _logger = logger;
        }

        public async Task Handle(Command command, CancellationToken cancellationToken)
        {
            var playlistIds = _options.DeletePrivatePlaylistItemsPlaylistIds
                .Split(',')
                .Select(rawId => new PlaylistId(rawId))
                .ToArray();

            foreach (var playlistId in playlistIds)
            {
                var playlistItems = await _playlistItemRepository.GetPrivatePlaylistItemsOfPlaylist(playlistId);
                var playlistItemIds = playlistItems.Select(playlistItem => playlistItem.Id).ToHashSet();
                _logger.LogInformation($"{playlistItemIds.Count} playlist items are marked as private");

                foreach (var playlistItemId in playlistItemIds)
                    await _playlistItemRepository.DeletePlaylistItem(playlistItemId);
            }
        }
    }
}
