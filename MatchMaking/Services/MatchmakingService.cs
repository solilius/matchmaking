using System.Text.Json;
using MatchMaking.Interfaces;
using Matchmaking.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Matchmaking.Services;

public class MatchmakingService
{
    private readonly IPlayerRepository _playerRepository; // Inject PlayerRepository
    private readonly IDatabase _redisDb;
    private readonly RedisKeys _keys;

    public MatchmakingService(IPlayerRepository playerRepository, IConnectionMultiplexer redis,
        IOptions<RedisSettings> redisSettings)
    {
        _playerRepository = playerRepository;
        _redisDb = redis.GetDatabase();
        _keys = redisSettings.Value.RedisKeys;
    }

    public async Task AddToPlayerQueueAsync(string playerId, string selectedHero)
    {
        var player = await _playerRepository.GetPlayerAsync(_redisDb, playerId);

        var queuedAt = DateTimeOffset.UtcNow;
        var timestamp = queuedAt.ToUnixTimeSeconds();

        var transaction = _redisDb.CreateTransaction();
        
        transaction.SortedSetAddAsync(_keys.PlayersQueueKey, FormatMemberValue(playerId, player.SkillRating), timestamp);
        transaction.SortedSetAddAsync(_keys.TasksQueueKey, FormatQueuedPlayer(player, selectedHero, queuedAt), timestamp);
        
        player.Status = PlayerStatus.SearchingMatch; // ew
        _playerRepository.SavePlayerAsync(transaction, player);
        
        await transaction.ExecuteAsync();
    }

    public async Task<PlayerQueueStatus> GetPlayerQueueStatus(string playerId)
    {
        var player = await _playerRepository.GetPlayerAsync(_redisDb, playerId);
        if (player.Status == PlayerStatus.FoundMatch)
        {
            var matchId = ""; // _matchRepository.MatchAsync(playerId);
            return new PlayerQueueStatus(PlayerStatus.FoundMatch, matchId);
        }
        
        return new PlayerQueueStatus(player.Status);
    }

    public async Task RemovePlayerFromQueueAsync(string playerId)
    {
        var player = await _playerRepository.GetPlayerAsync(_redisDb, playerId);
        var isSuccess = await _redisDb.SortedSetRemoveAsync(_keys.TasksQueueKey, FormatMemberValue(playerId, player.SkillRating));
        // remove from tasks
        // update player
        // throw if failed(?)
    }

    public Task ProcessFindMatch(QueuedPlayer queuedPlayer)
    {
        Console.WriteLine($"Processing player: {queuedPlayer.PlayerId}");
        return Task.CompletedTask;
    }
    
    private string FormatMemberValue(string playerId, int rating)
    {
        return $"{playerId}:{rating}";
    }

    private string FormatQueuedPlayer(Player player, string selectedHero, DateTimeOffset  queuedAt)
    {
        var queuedPlayer = new QueuedPlayer
        {
            PlayerId = player.Id,
            SelectedHero = selectedHero,
            CurrentRating = player.SkillRating,
            QueuedAt = queuedAt,
            SkillRangeExpansion = CalculateSkillRangeExpansion(queuedAt)
        };

        return JsonSerializer.Serialize(queuedPlayer);
    }

    private float CalculateSkillRangeExpansion(DateTimeOffset queuedAt)
    {
        // TODO: Implement
        return 0;
    }
}