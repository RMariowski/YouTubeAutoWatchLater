using MediatR;
using YouTubeAutoWatchLater.Application.Settings;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Application.Handlers;

public class DeletePrivatePlaylistItems
{
    public record Command : IRequest;

    public class Handler : IRequestHandler<Command>
    {
        private readonly IPlaylistItemRepository _playlistItemRepository;
        private readonly ISettings _settings;

        public Handler(IPlaylistItemRepository playlistItemRepository,
            ISettings settings)
        {
            _playlistItemRepository = playlistItemRepository;
            _settings = settings;
        }

        public async Task<Unit> Handle(Command command, CancellationToken cancellationToken)
        {
            await _playlistItemRepository.DeletePrivatePlaylistItemsOfPlaylist(new PlaylistId(_settings.PlaylistId));
            return Unit.Value;
        }
    }
}
