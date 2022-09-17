using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Application.Google;
using YouTubeAutoWatchLater.Application.YouTube.Options;
using YouTubeAutoWatchLater.Application.YouTube.Repositories;
using YouTubeAutoWatchLater.Application.YouTube.Services;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Application.YouTube;

internal static class Extensions
{
    public static IServiceCollection AddYouTube(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<YouTubeOptions>().Bind(configuration.GetSection("YouTube"));

        return services
            .AddSingleton(CreateYouTubeService)
            .AddSingleton<ISubscriptionRepository, YouTubeSubscriptionRepository>()
            .AddSingleton<IChannelRepository, YouTubeChannelRepository>()
            .AddSingleton<IPlaylistItemRepository, YouTubePlaylistItemRepository>()
            .AddSingleton<IPlaylistRuleResolver, PlaylistRuleResolver>();
    }

    // TODO: Would be good to make it somehow asynchronous
    private static YouTubeService CreateYouTubeService(IServiceProvider serviceProvider)
    {
        var googleApi = serviceProvider.GetRequiredService<IGoogleApi>();
        var logger = serviceProvider.GetRequiredService<ILogger<YouTubeService>>();

        logger.LogInformation("Getting access token");
        var accessToken = googleApi.GetAccessToken().GetAwaiter().GetResult();
        logger.LogInformation("Finished getting access token");

        logger.LogInformation("Creating YouTube Service");
        var youTubeService = googleApi.CreateYouTubeService(accessToken);
        logger.LogInformation("Finished creating YouTube API");

        return youTubeService;
    }
}
