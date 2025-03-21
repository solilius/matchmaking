using MatchMaking.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Matchmaking.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Matchmaking.Controllers;

[ApiController]
[Route("match")]
public class MatchController(
    IRepository<Match> matchRepository,
    IRepository<Player> playerRepository,
    IOptions<RedisSettings> redisSettings,
    IConnectionMultiplexer redis) : ControllerBase
{
    // TODO: ADD VALIDATIONS

    private readonly IDatabase _redisDb = redis.GetDatabase();

    [HttpPost("start/{matchId}")]
    public async Task<IActionResult> StartMatch(string matchId)
    {
        try
        {
            var match = await matchRepository.GetAsync(_redisDb, matchId);
            var isSuccess = await matchRepository.SaveAsync(_redisDb, match!.UpdateStatus(MatchStatus.Ongoing));
            if (isSuccess) return Ok(new { status = 1, message = "Match started." });

            throw new Exception("Failed to start match.");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, new { status = 0, message = "Failed start match." });
        }
    }

    [HttpGet("status/{matchId}")]
    public async Task<IActionResult> GetMatchStatus(string matchId)
    {
        try
        {
            var match = await matchRepository.GetAsync(_redisDb, matchId);
            return Ok(new { status = match.Status });
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, new { status = 0, message = "Failed to get match status." });
        }
    }

    [HttpPost("complete/{matchId}")]
    public async Task<IActionResult> CompleteMatch(string matchId, [FromBody] MatchResult result)
    {
        try
        {
            await HandleMatchCompleted(matchId, result);
            return Ok(new { status = 1 });
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, new { status = 0, message = "Failed to conclude match." });
        }
    }

    private async Task HandleMatchCompleted(string matchId, MatchResult results)
    {
        var transaction = _redisDb.CreateTransaction();

        foreach (var matchPlayer in results.Match.Players)
        {
            var player = await playerRepository.GetAsync(_redisDb, matchPlayer.PlayerId);
            var rateModifier = CalculatePlayerRating(matchPlayer.PlayerId, results);
            playerRepository.EnqueueSaveAsync(transaction, player.MatchCompleted(rateModifier));
            transaction.KeyDeleteAsync($"{redisSettings.Value.RedisKeys.PlayerMatchKey}:{matchPlayer.PlayerId}");
        }

        matchRepository.EnqueueRemoveAsync(transaction, matchId);

        await transaction.ExecuteAsync();
    }

    private int CalculatePlayerRating(string playerId, MatchResult results)
    {
        if (results.Match.ContainsBot) return 0;
        var diff = 10; // some logic
        return playerId == results.WinnerId ? diff : -diff;
    }
}