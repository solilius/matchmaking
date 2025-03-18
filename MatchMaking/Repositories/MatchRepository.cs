using Matchmaking.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Matchmaking.Repositories;

public class MatchRepository
{
    private readonly IDatabase _redisDb;
    private readonly RedisKeys _keys;

    public MatchRepository(IConnectionMultiplexer redis, IOptions<RedisSettings> redisSettings)
    {
        _redisDb = redis.GetDatabase();
        _keys = redisSettings.Value.RedisKeys;
    }
    
    public async Task SaveMatchAsync()
    {
    }
    
    public async Task<Player?> GetPlayerAsync()
    {
       return null;
    }
}