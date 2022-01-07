using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(YouTubeAutoWatchLater.Startup))]

namespace YouTubeAutoWatchLater;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services
            .AddHttpClient()
            .AddSingleton<ISettings, Settings>()
            .AddSingleton<IGoogleApis, GoogleApis>();
    }
}
