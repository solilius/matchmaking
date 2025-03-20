using System.Text.Json;
using MatchMaking.Interfaces;
using Matchmaking.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Matchmaking.Services;

public partial class MatchmakingService(
    IRepository<Player> playerRepository,
    IRepository<Match> matchRepository,
    IConnectionMultiplexer redis,
    IOptions<RedisSettings> redisSettings,
    IOptions<MatchmakingConfig> matchmakingConfig)
{
    private const string HeroHashField = "selectedHero";
    
    private readonly IDatabase _redisDb = redis.GetDatabase();
    private readonly MatchmakingConfig _matchmakingConfig = matchmakingConfig.Value;
    private readonly string _lobbyKey = redisSettings.Value.RedisKeys.LobbyKey;
    private readonly string _queueKey = redisSettings.Value.RedisKeys.QueueKey;
    
    public async Task AddToPlayerQueueAsync(string playerId, string selectedHero)
    {
        var player = await playerRepository.GetAsync(_redisDb, playerId);
        if (player is null) throw new KeyNotFoundException($"Player {playerId} not found");

        var queuedAt = DateTimeOffset.UtcNow;
        var timestamp = queuedAt.ToUnixTimeSeconds();
        var score = player.SkillRating + timestamp / Math.Pow(10, timestamp.ToString().Length);
        
        var transaction = _redisDb.CreateTransaction();
        transaction.SortedSetAddAsync(_lobbyKey, playerId, score);
        transaction.HashSetAsync( $"{_lobbyKey}:{playerId}", HeroHashField, selectedHero);
        transaction.SortedSetAddAsync(_queueKey, FormatQueuedPlayer(player, selectedHero, queuedAt), timestamp);

        player.UpdateStatus(PlayerStatus.SearchingMatch);
        playerRepository.SaveAsync(transaction, player);

        bool isSuccess = await transaction.ExecuteAsync();
        if (!isSuccess) throw new ApplicationException($"Failed to add player {playerId} to queue");
    }

    public async Task<PlayerQueueStatus> GetPlayerQueueStatus(string playerId)
    {
        var player = await playerRepository.GetAsync(_redisDb, playerId);
        
        if (player.Status == PlayerStatus.FoundMatch)
        {
            var key = GetKey($"{redisSettings.Value.RedisKeys.MatchesKey}*{playerId}*");
            var matchId = key.Split(":")[1];
            return new PlayerQueueStatus(PlayerStatus.FoundMatch, matchId);
        }

        return new PlayerQueueStatus(player.Status);
    }

    public async Task RemovePlayerFromQueueAsync(string playerId)
    {
        try
        {
            var player = await playerRepository.GetAsync(_redisDb, playerId);
            var transaction = _redisDb.CreateTransaction();

            transaction.SortedSetRemoveAsync(_lobbyKey, playerId);
            transaction.KeyDeleteAsync($"{_lobbyKey}:{playerId}");

            playerRepository.SaveAsync(transaction, player.UpdateStatus(PlayerStatus.Idle));

            await transaction.ExecuteAsync();

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    private string FormatMemberValue(string playerId, long timestamp) => $"{playerId}:{timestamp}";

    private string FormatQueuedPlayer(Player player, string selectedHero, DateTimeOffset queuedAt)
    {
        var queuedPlayer = new QueuedPlayer(player.Id,selectedHero,queuedAt);
        return JsonSerializer.Serialize(queuedPlayer);
    }

    private string GetKey(string pattern)
    {
        var endpoint = redis.GetEndPoints().First();
        var server = redis.GetServer(endpoint);
        
        return server.Keys(pattern: pattern).FirstOrDefault().ToString();
    }
}