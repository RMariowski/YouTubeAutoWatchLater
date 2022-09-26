using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using YouTubeAutoWatchLater.Application;
using YouTubeAutoWatchLater.Application.Repositories;
using YouTubeAutoWatchLater.Azure;
using YouTubeAutoWatchLater.Azure.Repositories;

[assembly: FunctionsStartup(typeof(Startup))]

namespace YouTubeAutoWatchLater.Azure;

public sealed  class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;

        builder.Services
            .AddApplication(configuration)
            .AddSingleton<IConfigurationRepository, ConfigurationTableStorageRepository>()
            .AddSingleton<IAutoAddedVideosRepository, AutoAddedVideosTableStorageRepository>();
    }
}
