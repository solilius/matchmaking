using System.Text.Json;
using Matchmaking.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace MatchMaking.Workers;

public class MatcherWorker : IHostedService
{
    private readonly IDatabase _redisDb;
    private readonly Func<QueuedPlayer, Task> _processTask;
    private readonly string _queueKey;
    private readonly int _batchSize;
    private CancellationTokenSource? _internalCts;
    private Task? _backgroundTask;

    public MatcherWorker(IDatabase redisDb, string queueKey, int processBatchSize, Func<QueuedPlayer, Task> processTask)
    {
        _queueKey = queueKey;
        _batchSize = processBatchSize;
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
            await _internalCts.CancelAsync();
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
            try
            {
                var tasks = await GetQueuedPlayers(_batchSize);
                if (tasks.Length > 0) await Task.WhenAll(tasks.Select(task => _processTask(task)));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            finally
            {
                await Task.Delay(500, token);
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