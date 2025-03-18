using Microsoft.AspNetCore.Mvc;
using Matchmaking.Models;
using Matchmaking.Services;

namespace Matchmaking.Controllers;

[ApiController]
[Route("matchmaking")]
public class MatchmakingController : ControllerBase
{
    private readonly MatchmakingService _matchmakingService;

    public MatchmakingController(MatchmakingService matchmakingService)
    {
        _matchmakingService = matchmakingService;
    }

    [HttpPost("queue")]
    public async Task<IActionResult> QueueForMatch([FromBody] QueueRequest request)
    {
        await _matchmakingService.AddToQueueAsync(request.PlayerId);
        return Ok(new { status = 1, message = "Player added to queue." });
    }
    
    [HttpGet("queue/{playerId}")]
    public async Task<IActionResult> GetPlayerStatus(string playerId)
    {
        var isQueued = await _matchmakingService.IsPlayerInQueueAsync(playerId);

        if (!isQueued)
            return NotFound(new { status = 0, message = "Player not found in queue." });

        return Ok(new { status = 1, message = "Player is in the queue." });
    }
}