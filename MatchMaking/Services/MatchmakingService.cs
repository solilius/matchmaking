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

    public async Task<bool> AddToPlayerQueueAsync(string playerId, string selectedHero)
    {
        var player = await _playerRepository.GetPlayerAsync(playerId);
        if (player is null) return false;

        player.Status = PlayerStatus.SearchingMatch; // ew
        var queuedAt = DateTimeOffset.UtcNow;
        var timestamp = queuedAt.ToUnixTimeSeconds();
        
        var addPlayerToQueueTask =  _redisDb.SortedSetAddAsync(_keys.PlayersQueueKey, FormatMemberValue(playerId, player.SkillRating), timestamp);
        var addTaskToQueue =  _redisDb.SortedSetAddAsync(_keys.TasksQueueKey, FormatQueuedPlayer(player, selectedHero, queuedAt), timestamp);
        var updatePlayerTask = _playerRepository.SavePlayerAsync(player);

        var results = await Task.WhenAll(addPlayerToQueueTask, addTaskToQueue, updatePlayerTask);
        
        // TODO: if any false rollback
        return results.All(v => v);
    }

    public async Task<PlayerQueueStatus> GetPlayerQueueStatus(string playerId)
    {
        // get player - status
        // get match
        return null;
    }

    public async Task<bool> RemovePlayerFromQueueAsync(string playerId)
    {
        var player = await _playerRepository.GetPlayerAsync(playerId);
        if (player is null) return false;

        var isSuccess = await _redisDb.SortedSetRemoveAsync(_keys.TasksQueueKey, FormatMemberValue(playerId, player.SkillRating));
        // remove from tasks
        // update player

        return isSuccess;
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