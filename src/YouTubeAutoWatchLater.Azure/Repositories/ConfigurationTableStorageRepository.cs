using System.Net;
using Azure;
using Azure.Data.Tables;
using YouTubeAutoWatchLater.Application.Repositories;

namespace YouTubeAutoWatchLater.Azure.Repositories;

public sealed class ConfigurationTableStorageRepository : IConfigurationRepository
{
    private const string TableName = "Configurations";
    private const string ValuePropKey = "Value";
    private const string GeneralPartitionKey = "General";
    private const string LastSuccessfulRun = nameof(LastSuccessfulRun);

    private readonly TableClient _tableClient;

    public ConfigurationTableStorageRepository()
    {
        var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        _tableClient = new TableClient(storageConnectionString, TableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<DateTime> GetLastSuccessfulExecutionDateTime()
    {
        Response<TableEntity>? entityResponse = null;

        try
        {
            entityResponse = await _tableClient.GetEntityAsync<TableEntity>(GeneralPartitionKey, LastSuccessfulRun);
        }
        catch (RequestFailedException e)
        {
            if (e.Status != (int)HttpStatusCode.NotFound)
                throw;
        }

        var lastSuccessfulExecutionAsString = entityResponse?.Value.GetString(ValuePropKey);
        return string.IsNullOrWhiteSpace(lastSuccessfulExecutionAsString)
            ? DateTime.UtcNow
            : DateTimeOffset.Parse(lastSuccessfulExecutionAsString).UtcDateTime;
    }

    public async Task SetLastSuccessfulExecutionDateTimeToNow()
    {
        TableEntity entity = new(GeneralPartitionKey, LastSuccessfulRun)
        {
            [ValuePropKey] = DateTimeOffset.UtcNow.ToString("o")
        };
        await _tableClient.UpsertEntityAsync(entity);
    }
}
