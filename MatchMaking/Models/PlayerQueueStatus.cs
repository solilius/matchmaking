namespace Matchmaking.Models;

public class PlayerQueueStatus(PlayerStatus status, string? matchId = null)
{
    public readonly PlayerStatus Status = status;
    public readonly string? MatchId = matchId;
}
