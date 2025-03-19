using System.Text.Json;
using MatchMaking.Interfaces;
using Matchmaking.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Matchmaking.Repositories;

public class PlayerRepository(IOptions<RedisSettings> redisSettings) : IPlayerRepository
{
    private readonly RedisKeys _keys = redisSettings.Value.RedisKeys;
    
    public async Task<bool> SavePlayerAsync(IDatabaseAsync db, Player player)
    {
        var key = $"{_keys.PlayersKey}:{player.Id}";
        var json = JsonSerializer.Serialize(player);

        return await db.StringSetAsync(key, json);
    }
    
    public async Task<Player?> GetPlayerAsync(IDatabaseAsync db, string playerId)
    {
        var key = $"{_keys.PlayersKey}:{playerId}";
        var json = await db.StringGetAsync(key);

        if (json.IsNullOrEmpty) return null;

        return JsonSerializer.Deserialize<Player>(json!);
    }
}
