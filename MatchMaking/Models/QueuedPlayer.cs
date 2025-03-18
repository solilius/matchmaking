namespace Matchmaking.Models;

public class QueuedPlayer
{
    public string PlayerId { get; set; }
    public string SelectedHero { get; set; }
    public int CurrentRating { get; set; }
    public DateTimeOffset  QueuedAt { get; set; }
    public float SkillRangeExpansion { get; set; } = 0.05f;
}
