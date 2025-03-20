namespace Matchmaking.Models;

public class QueuedPlayer(string playerId, string selectedHero, DateTimeOffset  queuedAt)
{
    public string PlayerId { get; } = playerId;
    public string SelectedHero { get; } = selectedHero;
    public DateTimeOffset QueuedAt { get; } = queuedAt;
}
