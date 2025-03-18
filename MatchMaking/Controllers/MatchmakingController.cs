using Microsoft.AspNetCore.Mvc;
using Matchmaking.Models;
using Matchmaking.Services;

namespace Matchmaking.Controllers;

[ApiController]
[Route("matchmaking")]
public class MatchmakingController : ControllerBase
{
    private readonly MatchmakingService _matchmakingService;

    // TODO: ADD VALIDATIONS
    // TODO: ERROR HANDLING
    
    public MatchmakingController(MatchmakingService matchmakingService)
    {
        _matchmakingService = matchmakingService;
    }

    [HttpPost("queue")]
    public async Task<IActionResult> AddPlayerToQueue([FromBody] QueueRequest request)
    {
        bool isSuccess = await _matchmakingService.AddToPlayerQueueAsync(request.PlayerId, request.SelectedHero);
        
        if (isSuccess) return Ok(new { status = 1, message = "Player added to queue." });
        return StatusCode(500, new { status = 0, message = "Failed to add player to queue." });
    }
    
    [HttpGet("queue/{playerId}")]
    public async Task<IActionResult> GetPlayerStatus(string playerId)
    {
        PlayerQueueStatus res = await _matchmakingService.GetPlayerQueueStatus(playerId);

        return Ok(new { status = res.Status, matchId = res.MatchId });
    }
    
    [HttpDelete("queue/{playerId}")]
    public async Task<IActionResult> RemovePlayerFromQueue(string playerId)
    {
        bool isSuccess = await _matchmakingService.RemovePlayerFromQueueAsync(playerId);
        
        if (isSuccess) return Ok(new { status = 1, message = "Player removed from queue." });
        return StatusCode(500, new { status = 0, message = "Failed to remove player from queue." });
    }
}