using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace YouTubeAutoWatchLater.Extensions;

public static class DurableOrchestrationContextExtensions
{
    public static async Task CallInBatches<T>(this IDurableOrchestrationContext context,
        Func<T, Task> function, T[] inputs, int batchSize = 25, int batchDelay = 5)
    {
        var tasks = new List<Task>(inputs.Length);
        foreach (var input in inputs)
        {
            var task = function(input);
            tasks.Add(task);
            while (tasks.Count(t => !t.IsCompleted) >= batchSize)
            {
                await Task.WhenAll(tasks.Where(t => t.IsCompleted));
                var fireAt = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(batchDelay));
                await context.CreateTimer(fireAt, CancellationToken.None);
            }
        }
    }
}
