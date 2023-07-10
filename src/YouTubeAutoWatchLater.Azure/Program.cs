using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YouTubeAutoWatchLater.Azure.Repositories;
using YouTubeAutoWatchLater.Core;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Azure;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults((_, builder) =>
            {
                builder
                    .AddApplicationInsights()
                    .AddApplicationInsightsLogger();
            })
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                services
                    .AddCore(configuration)
                    .AddScoped<IConfigurationRepository, ConfigurationTableStorageRepository>()
                    .AddScoped<IAutoAddedVideosRepository, AutoAddedVideosTableStorageRepository>();
            })
            .Build();
        await host.RunAsync();
    }
}
