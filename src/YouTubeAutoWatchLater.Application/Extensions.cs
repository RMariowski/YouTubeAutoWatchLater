using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouTubeAutoWatchLater.Application.Google;
using YouTubeAutoWatchLater.Application.Handlers;
using YouTubeAutoWatchLater.Application.YouTube;

namespace YouTubeAutoWatchLater.Application;

public static class Extensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddLogging()
            .AddHttpClient()
            .AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(typeof(UpdateAutoWatchLater.Handler).Assembly))
            .AddGoogle(configuration)
            .AddYouTube(configuration);
    }
}
