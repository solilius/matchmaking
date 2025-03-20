namespace Matchmaking.Models;

public class MatchPlayer(string playerId, string selectedHero)
{
    public string PlayerId { get; set; } = playerId;
    public string SelectedHero { get; set; } = selectedHero;
}

public class CreateMatchPlayerOptions(Player player, QueuedPlayer queuedPlayer)
{
    public Player Player { get; set; } = player;
    public QueuedPlayer QueuedPlayer { get; set; } = queuedPlayer;
}