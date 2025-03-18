using System.Text.Json;
using MatchMaking.Interfaces;
using Matchmaking.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Matchmaking.Repositories;

public class PlayerRepository: IPlayerRepository
{
    private readonly IDatabase _redisDb;
    private readonly RedisKeys _keys;

    public PlayerRepository(IConnectionMultiplexer redis, IOptions<RedisSettings> redisSettings)
    {
        _redisDb = redis.GetDatabase();
        _keys = redisSettings.Value.RedisKeys;
    }
    
    public async Task<bool> SavePlayerAsync(Player player)
    {
        var key = $"{_keys.PlayersKey}:{player.Id}";
        var json = JsonSerializer.Serialize(player);

        return await _redisDb.StringSetAsync(key, json);
    }
    
    public async Task<Player?> GetPlayerAsync(string playerId)
    {
        var key = $"{_keys.PlayersKey}:{playerId}";
        var json = await _redisDb.StringGetAsync(key);

        if (json.IsNullOrEmpty) return null;

        return JsonSerializer.Deserialize<Player>(json!);
    }
}