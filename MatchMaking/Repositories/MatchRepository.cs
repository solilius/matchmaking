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

    public void EnqueueSaveAsync(ITransaction db, Match match)
    {
        var key = GetKey(match.Id);
        var json = JsonSerializer.Serialize(match);

        db.StringSetAsync(key, json);
        db.KeyExpireAsync(key, TimeSpan.FromSeconds(redisSettings.Value.MatchTTL));
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
    
    public void EnqueueRemoveAsync(ITransaction db, string matchId)
    {
        var key = GetKey(matchId);
        db.KeyDeleteAsync(key);
    }
    
    public string GetKey(string id)
    {
        return $"{_keys.MatchesKey}:{id}";
    }
}