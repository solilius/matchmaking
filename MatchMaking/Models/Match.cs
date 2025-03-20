namespace Matchmaking.Models;

public class Match(List<MatchPlayer> players,  bool containsBot)
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public List<MatchPlayer> Players { get; } = players;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public bool ContainsBot { get; } = containsBot;

    public void UpdateStatus(MatchStatus status)
    {
        Status = status;
    }
}

public enum MatchStatus
{
    Pending,
    Ongoing,
}