using Microsoft.AspNetCore.Mvc;
using Matchmaking.Models;
using Matchmaking.Services;

namespace Matchmaking.Controllers;

[ApiController]
[Route("matchmaking")]
public class MatchmakingController(MatchmakingService matchmakingService) : ControllerBase
{

    // TODO: ADD VALIDATIONS
    // TODO: ERROR HANDLING
    
    [HttpPost("queue")]
    public async Task<IActionResult> AddPlayerToQueue([FromBody] QueueRequest request)
    {
        try
        {
            await matchmakingService.AddToPlayerQueueAsync(request.PlayerId, request.SelectedHero);
            return Ok(new { status = 1, message = "Player added to queue." });
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, new { status = 0, message = "Failed to add player to queue." });
        }
    }

    [HttpGet("queue/{playerId}")]
    public async Task<IActionResult> GetPlayerStatus(string playerId)
    {
        try
        {
            PlayerQueueStatus res = await matchmakingService.GetPlayerQueueStatus(playerId);
            return Ok(new { status = res.Status, matchId = res.MatchId });
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, new { status = 0, message = "Failed to get player queue status." });
        }
    }

    [HttpDelete("queue/{playerId}")]
    public async Task<IActionResult> RemovePlayerFromQueue(string playerId)
    {
        try
        {
            await matchmakingService.RemovePlayerFromQueueAsync(playerId);
            return Ok(new { status = 1, message = "Player removed from queue." });
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, new { status = 0, message = "Failed to remove player from queue." });
        }
    }
}