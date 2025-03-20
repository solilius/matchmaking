using System.Text.Json;
using MatchMaking.Interfaces;
using Matchmaking.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Matchmaking.Repositories;

public class PlayerRepository(IOptions<RedisSettings> redisSettings) : IRepository<Player>
{
    private readonly RedisKeys _keys = redisSettings.Value.RedisKeys;
    
    public async Task<bool> SaveAsync(IDatabaseAsync db, Player player)
    {
        var key = GetKey(player.Id);
        var json = JsonSerializer.Serialize(player);

        return await db.StringSetAsync(key, json);
    }
    
    public void EnqueueSaveAsync(ITransaction db, Player player)
    {
        var key = GetKey(player.Id);
        var json = JsonSerializer.Serialize(player);

        db.StringSetAsync(key, json);
    }
    
    public async Task<Player?> GetAsync(IDatabaseAsync db, string playerId)
    {
        var json = await GetJsonAsync(db, playerId);
        if (json is null) return null;

        return JsonSerializer.Deserialize<Player>(json!);
    }

    public async Task<string?> GetJsonAsync(IDatabaseAsync db, string playerId)
    {
        var key = GetKey(playerId);
        var json = await db.StringGetAsync(key);

        if (json.IsNullOrEmpty) return null;

        return json;
    }

    public void EnqueueRemoveAsync(ITransaction db, string playerId)
    {
        var key = GetKey(playerId);
        db.KeyDeleteAsync(key);
    }
    
    public string GetKey(string id)
    {
        return $"{_keys.PlayersKey}:{id}";
    }
}
