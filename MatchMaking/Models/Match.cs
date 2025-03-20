namespace Matchmaking.Models;

public class Match(List<MatchPlayer> players,  bool containsBot)
{
    public string Id { get; } = string.Join("-", players.Select(p => p.PlayerId));

    public List<MatchPlayer> Players { get; } = players;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public bool ContainsBot { get; } = containsBot;

    public Match UpdateStatus(MatchStatus status)
    {
        Status = status;
        return this;
    }
}

public enum MatchStatus
{
    Pending,
    Ongoing,
    Completed,
}