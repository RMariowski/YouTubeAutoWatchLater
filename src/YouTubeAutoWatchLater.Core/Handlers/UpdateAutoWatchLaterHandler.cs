using System.Collections.Concurrent;
using System.Net;
using Google;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.YouTube.Services;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Core.Handlers;

public interface IUpdateAutoWatchLaterHandler
{
    Task HandleAsync();
}

internal sealed class UpdateAutoWatchLaterHandler : IUpdateAutoWatchLaterHandler
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IChannelRepository _channelRepository;
    private readonly IPlaylistItemRepository _playlistItemRepository;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IAutoAddedVideosRepository _autoAddedVideosRepository;
    private readonly IPlaylistRuleResolver _playlistRuleResolver;
    private readonly ILogger<UpdateAutoWatchLaterHandler> _logger;

    public UpdateAutoWatchLaterHandler(
        ISubscriptionRepository subscriptionRepository,
        IChannelRepository channelRepository,
        IPlaylistItemRepository playlistItemRepository,
        IConfigurationRepository configurationRepository,
        IAutoAddedVideosRepository autoAddedVideosRepository,
        IPlaylistRuleResolver playlistRuleResolver,
        ILogger<UpdateAutoWatchLaterHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _channelRepository = channelRepository;
        _playlistItemRepository = playlistItemRepository;
        _configurationRepository = configurationRepository;
        _autoAddedVideosRepository = autoAddedVideosRepository;
        _playlistRuleResolver = playlistRuleResolver;
        _logger = logger;
    }

    public async Task HandleAsync()
    {
        var subscriptions = await GetSubscriptions();
        await SetUploadsPlaylists(subscriptions);

        var dateTime = DateTimeOffset.UtcNow.AddDays(-20);
        var videosToAdd = await GetNewVideosOfSubscriptions(subscriptions, dateTime);
        await AddNewVideosToSubscriptionsPlaylists(videosToAdd);

        await SetLastSuccessfulExecutionDateTimeToNow();
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
            _logger.LogInformation("Getting videos auto added for last month of {Channel}", channel);
            var videosAutoAdded = await _autoAddedVideosRepository.GetAutoAddedVideosAsync(channel.Id);
            _logger.LogInformation("Finished getting videos auto added for last month of {Channel}", channel);

            _logger.LogInformation("Getting uploads playlist items of {Channel}", channel);
            var videosSinceDateTime = await _playlistItemRepository.GetVideos(channel.UploadsPlaylist!, dateTime);
            _logger.LogInformation("Finished getting uploads playlist items of {Channel}", channel);

            var videosToAdd = videosSinceDateTime
                .Where(video => videosAutoAdded.Contains(video.Id) is false)
                .ToArray();
            newVideos.TryAdd(channel.Id, videosToAdd);
        });

        _logger.LogInformation("Finished setting recent videos of subscriptions");

        return newVideos;
    }

    private async Task AddNewVideosToSubscriptionsPlaylists(ConcurrentDictionary<ChannelId, Video[]> videosToAdd)
    {
        if (videosToAdd.IsEmpty)
        {
            _logger.LogInformation("No videos to add");
            return;
        }

        _logger.LogInformation("Adding recent videos");

        foreach (var (channelId, videos) in videosToAdd)
        {
            foreach (var video in videos)
            {
                var playlistId = _playlistRuleResolver.Resolve(video);

                try
                {
                    _logger.LogInformation("Adding {Video} to playlist {PlaylistId}", video, playlistId);
                    await _playlistItemRepository.AddToPlaylist(playlistId, video);
                    _logger.LogInformation("Finished adding video {Video} to playlist {PlaylistId}",
                        video, playlistId);

                    _logger.LogInformation("Adding auto added videos of channel {ChannelId}", channelId);
                    await _autoAddedVideosRepository.AddAsync(channelId, video);
                    _logger.LogInformation("Finished adding auto added videos of channel {ChannelId}", channelId);
                }
                catch (GoogleApiException googleApiException)
                    when (googleApiException.HttpStatusCode == HttpStatusCode.Conflict)
                {
                    _logger.LogError(googleApiException, "Unable to add video {Video} to playlist {PlaylistId}",
                        video, playlistId);
                }
            }
        }

        _logger.LogInformation("Finished adding recent videos");
    }

    private async Task SetLastSuccessfulExecutionDateTimeToNow()
    {
        _logger.LogInformation("Setting last successful execution date time");
        await _configurationRepository.SetLastSuccessfulExecutionDateTimeToNowAsync();
        _logger.LogInformation("Finished setting last successful execution date time");
    }
}
