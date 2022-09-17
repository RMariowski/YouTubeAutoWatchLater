using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YouTubeAutoWatchLater.Application.Repositories;
using YouTubeAutoWatchLater.Application.YouTube.Options;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Application.Handlers;

public sealed class UpdateAutoWatchLater
{
    public sealed record Command : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IChannelRepository _channelRepository;
        private readonly IPlaylistItemRepository _playlistItemRepository;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly YouTubeOptions _options;
        private readonly ILogger<Handler> _logger;

        public Handler(ISubscriptionRepository subscriptionRepository, IChannelRepository channelRepository,
            IPlaylistItemRepository playlistItemRepository, IConfigurationRepository configurationRepository,
            IOptions<YouTubeOptions> options, ILogger<Handler> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _channelRepository = channelRepository;
            _playlistItemRepository = playlistItemRepository;
            _configurationRepository = configurationRepository;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<Unit> Handle(Command command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting subscriptions");
            var subscriptions = await _subscriptionRepository.GetMySubscriptions();
            _logger.LogInformation("Finished getting subscriptions");

            _logger.LogInformation("Setting uploads playlist for subscriptions");
            await _channelRepository.SetUploadsPlaylistForSubscriptions(subscriptions);
            _logger.LogInformation("Finished setting uploads playlist for subscriptions");

            _logger.LogInformation("Getting last successful execution date time");
            var lastSuccessfulExecutionDateTime = await _configurationRepository.GetLastSuccessfulExecutionDateTime();
            _logger.LogInformation(
                $"Finished getting last successful execution date time: {lastSuccessfulExecutionDateTime:o} UTC");

            _logger.LogInformation("Setting recent videos of subscriptions");
            await SetRecentVideosForSubscriptions(subscriptions, lastSuccessfulExecutionDateTime);
            _logger.LogInformation("Finished setting recent videos of subscriptions");

            await AddRecentVideosToPlaylist(subscriptions);

            _logger.LogInformation("Setting last successful execution date time");
            await _configurationRepository.SetLastSuccessfulExecutionDateTimeToNow();
            _logger.LogInformation("Finished setting last successful execution date time");

            return Unit.Value;
        }

        private async Task SetRecentVideosForSubscriptions(Subscriptions subscriptions, DateTimeOffset dateTime)
        {
            foreach (var (_, channel) in subscriptions)
            {
                _logger.LogInformation($"Getting uploads playlist items of {channel}");
                var recentVideos = await _playlistItemRepository.GetRecentVideos(channel.UploadsPlaylist!, dateTime);
                _logger.LogInformation($"Finished getting uploads playlist items of {channel}");

                channel.RecentVideos = recentVideos;
            }
        }

        private async Task AddRecentVideosToPlaylist(Subscriptions subscriptions)
        {
            var recentVideos = subscriptions.Values
                .SelectMany(subscription => subscription.RecentVideos!)
                .OrderByDescending(video => video.PublishedAt)
                .ThenByDescending(video => video.AddedToUploadPlaylistAt)
                .ToArray();

            if (recentVideos.Length == 0)
            {
                _logger.LogInformation("No videos to add");
                return;
            }

            _logger.LogInformation("Adding recent videos to playlist");

            PlaylistId playlistId = new(_options.PlaylistId);
            foreach (var video in recentVideos)
            {
                _logger.LogInformation($"Adding {video} to playlist");
                await _playlistItemRepository.AddToPlaylist(playlistId, video);
                _logger.LogInformation($"Finished video {video} to playlist");
            }

            _logger.LogInformation("Finished adding recent videos to playlist");
        }
    }
}
