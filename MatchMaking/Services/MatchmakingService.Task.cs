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

            if (player is null || player.Status != PlayerStatus.SearchingMatch) return;

            var transaction = _redisDb.CreateTransaction();

            transaction.AddCondition(Condition.StringEqual(playerRepository.GetKey(queuedPlayer.PlayerId),
                playerJson));

            var secondsPassed = (DateTimeOffset.UtcNow - queuedPlayer.QueuedAt).TotalSeconds;

            if (secondsPassed >= _matchmakingConfig.MaxQueueWaitSeconds)
            {
                HandleCreateMatch(transaction, [new(player, queuedPlayer.SelectedHero)], true);

                bool isSuccess = await transaction.ExecuteAsync();
                if (isSuccess) return;
            }

            var rating = player.SkillRating;
            var expansion = GetRatingExpansion(rating, secondsPassed);
            var eligiblePlayers =                                      // + 1 because we add timestamp to the score
                _redisDb.SortedSetRangeByScore(_lobbyKey, Math.Max(rating - expansion, 0), rating + expansion + 1);

            var filteredEligiblePlayers = eligiblePlayers
                .Where(p => p != queuedPlayer.PlayerId);

            foreach (var eligiblePlayerId in filteredEligiblePlayers)
            {
                var opponent = await playerRepository.GetAsync(_redisDb, eligiblePlayerId);
                if (opponent is null || opponent.Status != PlayerStatus.SearchingMatch) continue;
                
                var opponentHero =
                    await _redisDb.HashGetAsync($"{_lobbyKey}:{eligiblePlayerId}", HeroHashField);

                if (!opponentHero.HasValue) continue;


                HandleCreateMatch(
                    transaction,
                    [
                        new CreateMatchPlayerOptions(player, queuedPlayer.SelectedHero),
                        new CreateMatchPlayerOptions(opponent, opponentHero.ToString())
                    ],
                    false);

                bool isSuccess = await transaction.ExecuteAsync();
                if (isSuccess) return;
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await _redisDb.SortedSetAddAsync(_queueKey, JsonSerializer.Serialize(queuedPlayer), timestamp);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Add failure limit
            await _redisDb.SortedSetAddAsync(_queueKey, JsonSerializer.Serialize(queuedPlayer), timestamp);
        }
    }

    private void HandleCreateMatch(IDatabaseAsync db, List<CreateMatchPlayerOptions> players, bool withBot)
    {
        List<MatchPlayer> matchPlayers = new();

        foreach (var p in players)
        {
            matchPlayers.Add(new MatchPlayer(p.Player.Id, p.SelectedHero));
            db.SortedSetRemoveAsync(_lobbyKey, p.Player.Id);
            db.KeyDeleteAsync($"{_lobbyKey}:{p.Player.Id}");

            playerRepository.SaveAsync(db, p.Player.UpdateStatus(PlayerStatus.FoundMatch));
        }

        matchRepository.SaveAsync(db, new Match(matchPlayers, withBot));
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
}