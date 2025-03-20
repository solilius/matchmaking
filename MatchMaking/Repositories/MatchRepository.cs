using System.Text.Json;
using MatchMaking.Interfaces;
using Matchmaking.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Matchmaking.Repositories;

public class MatchRepository(IOptions<RedisSettings> redisSettings) : IRepository<Match>
{
    private readonly RedisKeys _keys = redisSettings.Value.RedisKeys;

    public async Task<bool> SaveAsync(IDatabaseAsync db, Match match)
    {
        var key = GetKey(match.Id);
        var json = JsonSerializer.Serialize(match);

        bool isSuccess = await db.StringSetAsync(key, json);
        if (isSuccess)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(redisSettings.Value.MatchTTL));
            return true;
        }

        return false;
    }

    public async Task<Match?> GetAsync(IDatabaseAsync db, string matchId)
    {
        var json = await GetJsonAsync( db, matchId);

        if (json is null) return null;

        return JsonSerializer.Deserialize<Match>(json!);
    }

    public async Task<string?> GetJsonAsync(IDatabaseAsync db, string matchId)
    {
        var key = GetKey(matchId);
        var json = await db.StringGetAsync(key);

        if (json.IsNullOrEmpty) return null;

        return json;
    }
    
    public async Task<bool> RemoveAsync(IDatabaseAsync db, string matchId)
    {
        var key = GetKey(matchId);
        
        return await db.KeyDeleteAsync(key);
    }
    
    public async Task<string?> GetMatchIdAsync(IConnectionMultiplexer redis, IDatabaseAsync db, string playerId)
    {
        var endpoint = redis.GetEndPoints().First();
        var server = redis.GetServer(endpoint);
        var key = server.Keys(pattern: $"{_keys.MatchesKey}*{playerId}*").FirstOrDefault();

        var json = await db.StringGetAsync(key);

        if (json.IsNullOrEmpty) return null;
        var match = JsonSerializer.Deserialize<Match>(json!);
        return match?.Id;
    }
    
    public string GetKey(string id)
    {
        return $"{_keys.MatchesKey}:{id}";
    }
}