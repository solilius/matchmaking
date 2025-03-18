namespace Matchmaking.Models;

public class PlayerQueueStatus
{
    public PlayerStatus Status { get; set; }
    public string? MatchId { get; set; }
}