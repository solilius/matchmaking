namespace Matchmaking.Models;

public class Player
{
    public string Id { get; set; }
    public string Username { get; set; }
    public int SkillRating { get; set; }
    public PlayerStatus Status { get; set; }
}

public enum PlayerStatus
{
    Idle = 0,
    SearchingMatch = 1,
    FoundMatch = 2,
    InMatch = 3,
}
