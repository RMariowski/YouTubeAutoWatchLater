using Azure.Data.Tables;
using YouTubeAutoWatchLater.Core;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Azure.Repositories;

public sealed class AutoAddedVideosTableStorageRepository : IAutoAddedVideosRepository
{
    private const string TableName = "AutoAddedVideos";

    private readonly TableClient _tableClient;

    public AutoAddedVideosTableStorageRepository()
    {
        var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        _tableClient = new TableClient(storageConnectionString, TableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task AddAsync(ChannelId channelId, Video video)
    {
        TableEntity entity = new(channelId.Value, video.Id.Value);
        await _tableClient.AddEntityAsync(entity);
    }

    public async Task<IReadOnlyList<VideoId>> GetAutoAddedVideosAsync(ChannelId channelId)
    {
        var query = _tableClient.QueryAsync<TableEntity>(
            $"PartitionKey eq '{channelId.Value}'", Consts.MaxResults);

        List<VideoId> videos = [];
        await foreach (var page in query.AsPages())
        {
            var videoIds = page.Values.Select(entity => new VideoId(entity.RowKey));
            videos.AddRange(videoIds);
        }

        return videos;
    }

    public async Task DeleteOlderThanAsync(DateTimeOffset dateTime)
    {
        var filter = $"Timestamp le datetime'{dateTime:yyyy-MM-ddTHH:mm}'";
        var query = _tableClient.QueryAsync<TableEntity>(filter, Consts.MaxResults);

        await foreach (var page in query.AsPages())
        {
            foreach (var entity in page.Values)
            {
                await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
            }
        }
    }
}
