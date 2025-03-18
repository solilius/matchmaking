using System.Text.Json;
using Matchmaking.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace MatchMaking.Workers;

public class Worker : IHostedService
{
    private readonly IDatabase _redisDb;
    private readonly Func<QueuedPlayer, Task> _processTask;
    private readonly string _queueKey;
    private readonly int _batchSize;
    private CancellationTokenSource? _internalCts;
    private Task? _backgroundTask;

    public Worker(IDatabase redisDb, IOptions<RedisSettings> redisSettings, Func<QueuedPlayer, Task> processTask)
    {
        _queueKey = redisSettings.Value.RedisKeys.TasksQueueKey;
        _batchSize = redisSettings.Value.ProcessBatchSize;
        _redisDb = redisDb;
        _processTask = processTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundTask = Task.Run(() => WorkerLoopAsync(_internalCts.Token), _internalCts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_internalCts != null)
        {
            _internalCts.Cancel();
            try
            {
                if (_backgroundTask != null)
                {
                    await _backgroundTask;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
        }
    }

    private async Task WorkerLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var tasks = await GetQueuedPlayers(_batchSize);

            if (tasks.Length == 0)
            {
                await Task.Delay(500, token);
                continue;
            }

            foreach (var task in tasks)
            {
                await _processTask(task);
            }
        }
    }

    private async Task<QueuedPlayer[]> GetQueuedPlayers(int chunkSize)
    {
        var entries = await _redisDb.SortedSetPopAsync(_queueKey, chunkSize);
        var queuedPlayers = entries
            .Select(e => JsonSerializer.Deserialize<QueuedPlayer>(e.Element!)!)
            .ToArray();
        return queuedPlayers;
    }
}
