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
    private readonly RedisKeys _Keys = redisSettings.Value.RedisKeys;

    public async Task AddToPlayerQueueAsync(string playerId, string selectedHero)
    {
        var player = await playerRepository.GetAsync(_redisDb, playerId);
        if (await ShouldQueuePlayer(player)) throw new ApplicationException($"Invalid player to queue");

        if ((await _redisDb.SortedSetScoreAsync(_Keys.LobbyKey, playerId)).HasValue) return;

        var queuedAt = DateTimeOffset.UtcNow;
        var timestamp = queuedAt.ToUnixTimeSeconds();
        var score = player.SkillRating + timestamp / Math.Pow(10, timestamp.ToString().Length);

        var transaction = _redisDb.CreateTransaction();
        transaction.SortedSetAddAsync(_Keys.LobbyKey, playerId, score);
        transaction.HashSetAsync($"{_Keys.LobbyKey}:{playerId}", HeroHashField, selectedHero);
        transaction.SortedSetAddAsync(_Keys.QueueKey, FormatQueuedPlayer(player, selectedHero, queuedAt), timestamp);

        player.UpdateStatus(PlayerStatus.SearchingMatch);
        playerRepository.EnqueueSaveAsync(transaction, player);

        bool isSuccess = await transaction.ExecuteAsync();
        if (!isSuccess) throw new ApplicationException($"Failed to add player {playerId} to queue");
    }

    public async Task<PlayerQueueStatus> GetPlayerQueueStatus(string playerId)
    {
        var player = await playerRepository.GetAsync(_redisDb, playerId);
        if (player is null) throw new KeyNotFoundException($"Player {playerId} not found");

        if (player.Status == PlayerStatus.FoundMatch)
        {
            var matchId = await _redisDb.StringGetAsync($"{_Keys.PlayerMatchKey}:{playerId}");
            return new PlayerQueueStatus(PlayerStatus.FoundMatch, matchId.ToString());
        }

        return new PlayerQueueStatus(player.Status);
    }

    public async Task RemovePlayerFromQueueAsync(string playerId)
    {
        var player = await playerRepository.GetAsync(_redisDb, playerId);
        if (player is null) throw new KeyNotFoundException($"Player {playerId} not found");

        var transaction = _redisDb.CreateTransaction();

        transaction.SortedSetRemoveAsync(_Keys.LobbyKey, playerId);
        transaction.KeyDeleteAsync($"{_Keys.LobbyKey}:{playerId}");

        playerRepository.EnqueueSaveAsync(transaction, player.UpdateStatus(PlayerStatus.Idle));

        await transaction.ExecuteAsync();
    }

    private string FormatQueuedPlayer(Player player, string selectedHero, DateTimeOffset queuedAt)
    {
        var queuedPlayer = new QueuedPlayer(player.Id, selectedHero, queuedAt);
        return JsonSerializer.Serialize(queuedPlayer);
    }

    private async Task<bool> ShouldQueuePlayer(Player? player)
    {
        return player is null || player.Status == PlayerStatus.SearchingMatch ||
               (await _redisDb.SortedSetScoreAsync(_Keys.LobbyKey, player.Id)).HasValue;
    }
}