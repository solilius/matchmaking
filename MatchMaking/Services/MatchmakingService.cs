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
        var memberValue = FormatMemberValue(playerId, timestamp);
        
        var transaction = _redisDb.CreateTransaction();
        transaction.SortedSetAddAsync(_lobbyKey, memberValue, player.SkillRating);
        transaction.HashSetAsync( $"{_lobbyKey}:{memberValue}", HeroHashField, selectedHero);
        transaction.SortedSetAddAsync(_queueKey, FormatQueuedPlayer(player, selectedHero, queuedAt), timestamp);

        player.UpdateStatus(PlayerStatus.SearchingMatch);
        playerRepository.SaveAsync(transaction, player);

        bool isSuccess = await transaction.ExecuteAsync();
        if (!isSuccess) throw new ApplicationException($"Failed to add player {playerId} to queue");
    }

    public async Task<PlayerQueueStatus> GetPlayerQueueStatus(string playerId)
    {
        var player = await playerRepository.GetAsync(_redisDb, playerId);
        
        // if (player.Status == PlayerStatus.FoundMatch)
        // {
        //     var matchId = await matchRepository.GetMatchIdAsync(redis, _redisDb, playerId);
        //     return new PlayerQueueStatus(PlayerStatus.FoundMatch, matchId);
        // }

        return new PlayerQueueStatus(player.Status);
    }

    public async Task RemovePlayerFromQueueAsync(string playerId)
    {
        var player = await playerRepository.GetAsync(_redisDb, playerId);
        // remove from lobby    
        // remove from tasks
        // update player
        // throw if failed(?)
    }
    
    private string FormatMemberValue(string playerId, long timestamp) => $"{playerId}:{timestamp}";

    private string FormatQueuedPlayer(Player player, string selectedHero, DateTimeOffset queuedAt)
    {
        var queuedPlayer = new QueuedPlayer(player.Id,selectedHero,queuedAt);
        return JsonSerializer.Serialize(queuedPlayer);
    }
}