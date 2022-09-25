using MediatR;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Application.Repositories;
using YouTubeAutoWatchLater.Application.YouTube.Services;
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
        private readonly IPlaylistRuleResolver _playlistRuleResolver;
        private readonly ILogger<Handler> _logger;

        public Handler(ISubscriptionRepository subscriptionRepository, IChannelRepository channelRepository,
            IPlaylistItemRepository playlistItemRepository, IConfigurationRepository configurationRepository,
            IPlaylistRuleResolver playlistRuleResolver, ILogger<Handler> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _channelRepository = channelRepository;
            _playlistItemRepository = playlistItemRepository;
            _configurationRepository = configurationRepository;
            _playlistRuleResolver = playlistRuleResolver;
            _logger = logger;
        }

        public async Task<Unit> Handle(Command command, CancellationToken cancellationToken)
        {
            var subscriptions = await GetSubscriptions();
            await SetUploadsPlaylists(subscriptions);

            var lastSuccessfulExecutionDateTime = await GetLastSuccessfulExecutionDateTime();

            await SetRecentVideosForSubscriptions(subscriptions, lastSuccessfulExecutionDateTime);
            await AddRecentVideosToPlaylist(subscriptions);

            await SetLastSuccessfulExecutionDateTimeToNow();

            return Unit.Value;
        }

        private async Task<Subscriptions> GetSubscriptions()
        {
            _logger.LogInformation("Getting subscriptions");
            var subscriptions = await _subscriptionRepository.GetMySubscriptions();
            _logger.LogInformation("Finished getting subscriptions");

            return subscriptions;
        }

        private async Task SetUploadsPlaylists(Subscriptions subscriptions)
        {
            _logger.LogInformation("Setting uploads playlist for subscriptions");

            var channelIds = subscriptions.Select(subscription => subscription.Key).ToArray();
            var uploadsPlaylists = await _channelRepository.GetUploadsPlaylists(channelIds);

            foreach (var uploadsPlaylist in uploadsPlaylists)
                subscriptions[uploadsPlaylist.Key].UploadsPlaylist = uploadsPlaylist.Value;

            _logger.LogInformation("Finished setting uploads playlist for subscriptions");
        }

        private async Task<DateTime> GetLastSuccessfulExecutionDateTime()
        {
            _logger.LogInformation("Getting last successful execution date time");
            var lastSuccessfulExecutionDateTime = await _configurationRepository.GetLastSuccessfulExecutionDateTime();
            _logger.LogInformation(
                $"Finished getting last successful execution date time: {lastSuccessfulExecutionDateTime:o} UTC");

            return lastSuccessfulExecutionDateTime;
        }

        private async Task SetRecentVideosForSubscriptions(Subscriptions subscriptions, DateTimeOffset dateTime)
        {
            _logger.LogInformation("Setting recent videos of subscriptions");

            ParallelOptions options = new() { MaxDegreeOfParallelism = 10 };

            await Parallel.ForEachAsync(subscriptions.Values, options, async (channel, _) =>
            {
                _logger.LogInformation($"Getting uploads playlist items of {channel}");
                var recentVideos = await _playlistItemRepository.GetRecentVideos(channel.UploadsPlaylist!, dateTime);
                _logger.LogInformation($"Finished getting uploads playlist items of {channel}");

                channel.RecentVideos = recentVideos;
            });

            _logger.LogInformation("Finished setting recent videos of subscriptions");
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

            foreach (var video in recentVideos)
            {
                var playlistId = _playlistRuleResolver.Resolve(video);

                _logger.LogInformation($"Adding {video} to playlist {playlistId}");
                await _playlistItemRepository.AddToPlaylist(playlistId, video);
                _logger.LogInformation($"Finished video {video} to playlist {playlistId}");
            }

            _logger.LogInformation("Finished adding recent videos to playlist");
        }

        private async Task SetLastSuccessfulExecutionDateTimeToNow()
        {
            _logger.LogInformation("Setting last successful execution date time");
            await _configurationRepository.SetLastSuccessfulExecutionDateTimeToNow();
            _logger.LogInformation("Finished setting last successful execution date time");
        }
    }
}
