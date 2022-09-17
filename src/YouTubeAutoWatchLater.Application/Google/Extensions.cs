using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouTubeAutoWatchLater.Application.Google.Options;

namespace YouTubeAutoWatchLater.Application.Google;

internal static class Extensions
{
    public static IServiceCollection AddGoogle(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GoogleOptions>().Bind(configuration.GetSection("Google"));
        return services.AddSingleton<IGoogleApi, GoogleApi>();
    }
}
