using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YouTubeAutoWatchLater.Core.Google;

internal static class Extensions
{
    internal static IServiceCollection AddGoogle(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GoogleOptions>().Bind(configuration.GetSection("Google"));
        return services.AddScoped<IGoogleApi, GoogleApi>();
    }
}
