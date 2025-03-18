using StackExchange.Redis;

namespace Matchmaking.Services;

public class MatchmakingService
{
    private readonly IDatabase _redisDb;
    private const string QueueKey = "matchmaking:queue";

    public MatchmakingService(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    public async Task AddToQueueAsync(string playerId)
    {
        await _redisDb.ListRightPushAsync(QueueKey, playerId);
    }

    public async Task<bool> IsPlayerInQueueAsync(string playerId)
    {
        var players = await _redisDb.ListRangeAsync(QueueKey);
        return players.Any(p => p.ToString() == playerId);
    }
}