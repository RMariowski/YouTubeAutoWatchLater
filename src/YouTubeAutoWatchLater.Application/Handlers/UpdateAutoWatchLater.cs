using System.Collections.Concurrent;
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
        private readonly IAutoAddedVideosRepository _autoAddedVideosRepository;
        private readonly IPlaylistRuleResolver _playlistRuleResolver;
        private readonly ILogger<Handler> _logger;

        public Handler(ISubscriptionRepository subscriptionRepository, IChannelRepository channelRepository,
            IPlaylistItemRepository playlistItemRepository, IConfigurationRepository configurationRepository,
            IAutoAddedVideosRepository autoAddedVideosRepository, IPlaylistRuleResolver playlistRuleResolver,
            ILogger<Handler> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _channelRepository = channelRepository;
            _playlistItemRepository = playlistItemRepository;
            _configurationRepository = configurationRepository;
            _autoAddedVideosRepository = autoAddedVideosRepository;
            _playlistRuleResolver = playlistRuleResolver;
            _logger = logger;
        }

        public async Task<Unit> Handle(Command command, CancellationToken cancellationToken)
        {
            var subscriptions = await GetSubscriptions();
            await SetUploadsPlaylists(subscriptions);

            var dateTime = DateTimeOffset.UtcNow.AddDays(-20);
            var videosToAdd = await GetNewVideosOfSubscriptions(subscriptions, dateTime);
            await AddNewVideosToSubscriptionsPlaylists(videosToAdd.Values.SelectMany(videos => videos).ToArray());
            await UpdateAutoAddedVideos(videosToAdd);

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

        private async Task<ConcurrentDictionary<ChannelId, Video[]>> GetNewVideosOfSubscriptions(
            Subscriptions subscriptions, DateTimeOffset dateTime)
        {
            _logger.LogInformation("Setting recent videos of subscriptions");

            ConcurrentDictionary<ChannelId, Video[]> newVideos = new();
            ParallelOptions options = new() { MaxDegreeOfParallelism = 10 };
            await Parallel.ForEachAsync(subscriptions.Values, options, async (channel, _) =>
            {
                _logger.LogInformation($"Getting videos auto added for last month of {channel}");
                var videosAutoAdded = await _autoAddedVideosRepository.GetAutoAddedVideos(channel.Id);
                _logger.LogInformation($"Finished getting videos auto added for last month of {channel}");

                _logger.LogInformation($"Getting uploads playlist items of {channel}");
                var videosSinceDateTime = await _playlistItemRepository.GetVideos(channel.UploadsPlaylist!, dateTime);
                _logger.LogInformation($"Finished getting uploads playlist items of {channel}");

                var videosToAdd = videosSinceDateTime
                    .Where(video => videosAutoAdded.Contains(video.Id) is false)
                    .ToArray();
                newVideos.TryAdd(channel.Id, videosToAdd);
            });

            _logger.LogInformation("Finished setting recent videos of subscriptions");

            return newVideos;
        }

        private async Task AddNewVideosToSubscriptionsPlaylists(IReadOnlyCollection<Video> videos)
        {
            if (videos.Count == 0)
            {
                _logger.LogInformation("No videos to add");
                return;
            }

            _logger.LogInformation("Adding recent videos to playlist");

            foreach (var video in videos)
            {
                var playlistId = _playlistRuleResolver.Resolve(video);

                _logger.LogInformation($"Adding {video} to playlist {playlistId}");
                await _playlistItemRepository.AddToPlaylist(playlistId, video);
                _logger.LogInformation($"Finished adding video {video} to playlist {playlistId}");
            }

            _logger.LogInformation("Finished adding recent videos to playlist");
        }

        private async Task UpdateAutoAddedVideos(ConcurrentDictionary<ChannelId, Video[]> videosToAdd)
        {
            _logger.LogInformation("Updating auto added videos");

            foreach (var (channelId, videos) in videosToAdd)
            {
                _logger.LogInformation($"Adding auto added videos of channel {channelId}");
                await _autoAddedVideosRepository.Add(channelId, videos);
                _logger.LogInformation($"Finished adding auto added videos of channel {channelId}");
            }

            _logger.LogInformation("Finished updating auto added videos");
        }

        private async Task SetLastSuccessfulExecutionDateTimeToNow()
        {
            _logger.LogInformation("Setting last successful execution date time");
            await _configurationRepository.SetLastSuccessfulExecutionDateTimeToNow();
            _logger.LogInformation("Finished setting last successful execution date time");
        }
    }
}
