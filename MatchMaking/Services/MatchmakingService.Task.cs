using System.Text.Json;
using Matchmaking.Models;
using StackExchange.Redis;

namespace Matchmaking.Services;

public partial class MatchmakingService
{
    public async Task ProcessFindMatch(QueuedPlayer queuedPlayer)
    {
        try
        {
            var playerJson = await playerRepository.GetJsonAsync(_redisDb, queuedPlayer.PlayerId);
            var player = JsonSerializer.Deserialize<Player>(playerJson!);

            if (IsPlayerNotSearching(player)) return;


            if (HasQueuedTooLong(queuedPlayer))
            {
                var transaction = CreateWatchedTransaction(queuedPlayer.PlayerId, playerJson!);
                HandleCreateMatch(transaction, [new(player, queuedPlayer.SelectedHero)], true);
                await transaction.ExecuteAsync();
                return; // If ExecuteAsync returns false, throw to requeue in catch.
            }

            var eligibleOpponentIds = GetEligibleOpponentIds(player, queuedPlayer);

            foreach (var eligiblePlayerId in eligibleOpponentIds)
            {
                var opponentJson = await playerRepository.GetJsonAsync(_redisDb, eligiblePlayerId);
                var opponent = JsonSerializer.Deserialize<Player>(opponentJson!);
                if (IsPlayerNotSearching(opponent)) continue;

                var transaction =
                    CreateWatchedTransaction(queuedPlayer.PlayerId, playerJson!, eligiblePlayerId, opponentJson);

                if (await TryToMatch(transaction, player, queuedPlayer.SelectedHero, eligiblePlayerId))
                {
                    var isSuccess = await transaction.ExecuteAsync();
                    if (isSuccess) return;
                }
            }

            await Requeue(queuedPlayer);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            await Requeue(queuedPlayer); // Add failure limit
        }
    }

    private bool IsPlayerNotSearching(Player? player)
    {
        return (player is null || player.Status != PlayerStatus.SearchingMatch);
    }

    private ITransaction CreateWatchedTransaction(string playerId, string playerJson, string? opponentId = null,
        string? opponentJson = null)
    {
        var transaction = _redisDb.CreateTransaction();
        transaction.AddCondition(Condition.StringEqual(playerRepository.GetKey(playerId), playerJson));

        if (opponentJson != null && opponentId != null)
        {
            transaction.AddCondition(Condition.StringEqual(playerRepository.GetKey(opponentId), opponentJson));
        }

        return transaction;
    }

    private double GetSecondsSinceQueue(QueuedPlayer queuedPlayer)
    {
        return (DateTimeOffset.UtcNow - queuedPlayer.QueuedAt).TotalSeconds;
    }

    private bool HasQueuedTooLong(QueuedPlayer queuedPlayer)
    {
        var secondsPassed = GetSecondsSinceQueue(queuedPlayer);
        return secondsPassed >= _matchmakingConfig.MaxQueueWaitSeconds;
    }

    private List<string> GetEligibleOpponentIds(Player player, QueuedPlayer queuedPlayer)
    {
        var rating = player.SkillRating;
        var secondsPassed = GetSecondsSinceQueue(queuedPlayer);
        var expansion = GetRatingExpansion(rating, secondsPassed);
        var eligiblePlayers = // + 1 because we add timestamp to the score
            _redisDb.SortedSetRangeByScore(
                _lobbyKey,
                Math.Max(rating - expansion, 0),
                rating + expansion + 1,
                Exclude.None,
                Order.Ascending,
                0,
                200
            );

        return eligiblePlayers
            .Where(p => p != player.Id)
            .Select(p => p.ToString())
            .ToList();
    }

    private async Task<bool> TryToMatch(ITransaction transaction, Player player, string playerHero, string opponentId)
    {
        try
        {
            var opponent = await playerRepository.GetAsync(_redisDb, opponentId);
            if (opponent is null || opponent.Status != PlayerStatus.SearchingMatch) return false;

            var opponentHero = await _redisDb.HashGetAsync($"{_lobbyKey}:{opponentId}", HeroHashField);

            if (!opponentHero.HasValue) return false;

            HandleCreateMatch(transaction,
            [
                new CreateMatchPlayerOptions(player, playerHero),
                new CreateMatchPlayerOptions(opponent, opponentHero.ToString())
            ], false);
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return false;
        }
    }

    private void HandleCreateMatch(ITransaction transaction, List<CreateMatchPlayerOptions> players, bool withBot)
    {
        List<MatchPlayer> matchPlayers = new();

        foreach (var p in players)
        {
            matchPlayers.Add(new MatchPlayer(p.Player.Id, p.SelectedHero));
            transaction.SortedSetRemoveAsync(_lobbyKey, p.Player.Id);
            transaction.KeyDeleteAsync($"{_lobbyKey}:{p.Player.Id}");

            playerRepository.EnqueueSaveAsync(transaction, p.Player.UpdateStatus(PlayerStatus.FoundMatch));
        }

        matchRepository.EnqueueSaveAsync(transaction, new Match(matchPlayers, withBot));
    }

    private int GetRatingExpansion(int playerRating, double queueingSeconds)
    {
        var initialRateExpansion = Math.Min(
            _matchmakingConfig.MaxInitialRatingRangeExpand,
            playerRating * _matchmakingConfig.InitialRatingRangeExpand
        );

        var expansion = Math.Floor(queueingSeconds / _matchmakingConfig.SkillRangeExpansionIntervalSeconds) *
                        Math.Min(
                            playerRating * _matchmakingConfig.RatingRangeExpand,
                            _matchmakingConfig.MaxRatingRangeExpand
                        );

        return (int)(initialRateExpansion + expansion);
    }

    private async Task Requeue(QueuedPlayer queuedPlayer)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _redisDb.SortedSetAddAsync(_queueKey, JsonSerializer.Serialize(queuedPlayer), timestamp);
    }
}