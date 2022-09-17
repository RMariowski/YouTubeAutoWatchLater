using System.Net;
using Azure;
using Azure.Data.Tables;
using YouTubeAutoWatchLater.Application.Repositories;

namespace YouTubeAutoWatchLater.Azure.Repositories;

public sealed class ConfigurationTableStorageRepository : IConfigurationRepository
{
    private const string TableName = "Configurations";
    private const string ValuePropKey = "Value";
    private readonly (string PartitionKey, string RowKey) _lastSuccessfulRun = ("General", "LastSuccessfulRun");

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
            entityResponse = await _tableClient.GetEntityAsync<TableEntity>(
                _lastSuccessfulRun.PartitionKey, _lastSuccessfulRun.RowKey);
        }
        catch (RequestFailedException e)
        {
            if (e.Status != (int)HttpStatusCode.NotFound)
                throw;
        }

        var lastSuccessfulExecutionAsString = entityResponse?.Value.GetString(ValuePropKey);
        if (string.IsNullOrWhiteSpace(lastSuccessfulExecutionAsString))
            return DateTime.UtcNow;

        var lastSuccessfulExecution = DateTimeOffset.Parse(lastSuccessfulExecutionAsString);
        return lastSuccessfulExecution.UtcDateTime;
    }

    public async Task SetLastSuccessfulExecutionDateTimeToNow()
    {
        TableEntity entity = new(_lastSuccessfulRun.PartitionKey, _lastSuccessfulRun.RowKey)
        {
            [ValuePropKey] = DateTimeOffset.UtcNow.ToString("o")
        };
        await _tableClient.UpsertEntityAsync(entity);
    }
}
