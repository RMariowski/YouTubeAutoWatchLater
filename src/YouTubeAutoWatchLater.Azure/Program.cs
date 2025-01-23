using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Azure.Repositories;
using YouTubeAutoWatchLater.Core;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Azure;

internal class Program
{
    private static async Task Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddApplicationInsightsTelemetryWorkerService();
                services.ConfigureFunctionsApplicationInsights();

                var configuration = hostContext.Configuration;

                services
                    .AddCore(configuration)
                    .AddScoped<IConfigurationRepository, ConfigurationTableStorageRepository>()
                    .AddScoped<IAutoAddedVideosRepository, AutoAddedVideosTableStorageRepository>();
            })
            .ConfigureLogging(logging =>
            {
                logging.Services.Configure<LoggerFilterOptions>(options =>
                {
                    var defaultRule = options.Rules.FirstOrDefault(rule =>
                        rule.ProviderName ==
                        "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
                    if (defaultRule is not null)
                    {
                        options.Rules.Remove(defaultRule);
                    }
                });
            })
            .Build();
        await host.RunAsync();
    }
}
