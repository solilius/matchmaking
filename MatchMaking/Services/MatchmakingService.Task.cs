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
                HandleCreateMatch(transaction, [new(player, queuedPlayer)], true);

                bool isSuccess = await transaction.ExecuteAsync();
                if (isSuccess) return;
            }

            var rating = player.SkillRating;
            var expansion = GetRatingExpansion(rating, secondsPassed);
            var eligiblePlayers =
                _redisDb.SortedSetRangeByScore(_lobbyKey, Math.Max(rating - expansion, 0), rating + expansion);

            // TODO: use timestamp in score and search max +1 this way it will be sorted already
            var sortedEligiblePlayers = eligiblePlayers
                .Select(entry => entry.ToString().Split(':'))
                .Select(parts => new { PlayerId = parts[0], Timestamp = long.Parse(parts[1]) })
                .Where(p => p.PlayerId != queuedPlayer.PlayerId)
                .OrderBy(p => p.Timestamp);

            foreach (var eligiblePlayer in sortedEligiblePlayers)
            {
                var opponent = await playerRepository.GetAsync(_redisDb, eligiblePlayer.PlayerId);
                if (opponent is null || opponent.Status != PlayerStatus.SearchingMatch) continue;


                var opponentMemberValue = FormatMemberValue(eligiblePlayer.PlayerId, eligiblePlayer.Timestamp);

                var opponentHero =
                    await _redisDb.HashGetAsync($"{_lobbyKey}:{opponentMemberValue}", HeroHashField);

                if (!opponentHero.HasValue) continue;


                HandleCreateMatch(
                    transaction,
                    [
                        new CreateMatchPlayerOptions(player, queuedPlayer),
                        new CreateMatchPlayerOptions(opponent,
                            new QueuedPlayer(opponent.Id, opponentHero.ToString(),
                                DateTimeOffset.FromUnixTimeSeconds(eligiblePlayer.Timestamp)))
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
            var memberValue = FormatMemberValue(p.Player.Id, p.QueuedPlayer.QueuedAt.ToUnixTimeSeconds());
            matchPlayers.Add(new MatchPlayer(p.Player.Id, p.QueuedPlayer.SelectedHero));
            db.SortedSetRemoveAsync(_lobbyKey, memberValue);
            db.KeyDeleteAsync($"{_lobbyKey}:{memberValue}");

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