using MediatR;
using Microsoft.Extensions.Options;
using YouTubeAutoWatchLater.Application.YouTube.Options;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Application.Handlers;

public class DeletePrivatePlaylistItems
{
    public record Command : IRequest;

    public class Handler : IRequestHandler<Command>
    {
        private readonly IPlaylistItemRepository _playlistItemRepository;
        private readonly YouTubeOptions _options;

        public Handler(IPlaylistItemRepository playlistItemRepository,
            IOptions<YouTubeOptions> options)
        {
            _playlistItemRepository = playlistItemRepository;
            _options = options.Value;
        }

        public async Task<Unit> Handle(Command command, CancellationToken cancellationToken)
        {
            await _playlistItemRepository.DeletePrivatePlaylistItemsOfPlaylist(new PlaylistId(_options.PlaylistId));
            return Unit.Value;
        }
    }
}
