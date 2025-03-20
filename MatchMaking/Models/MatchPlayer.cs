namespace Matchmaking.Models;

public class MatchPlayer(string playerId, string selectedHero)
{
    public string PlayerId { get; set; } = playerId;
    public string SelectedHero { get; set; } = selectedHero;
}

public class CreateMatchPlayerOptions(Player player, string selectedHero)
{
    public Player Player { get; set; } = player;
    public string SelectedHero { get; set; } = selectedHero;
}