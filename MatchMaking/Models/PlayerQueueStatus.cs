namespace Matchmaking.Models;

public class PlayerQueueStatus(PlayerStatus status, string? matchId = null)
{
    public PlayerStatus Status { get; init; } = status;
    public string? MatchId { get; init; } = matchId;
}
