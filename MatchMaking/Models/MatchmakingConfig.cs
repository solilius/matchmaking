namespace Matchmaking.Models;

public class MatchmakingConfig
{
    public int ProcessBatchSize { get; set; }
    public int MaxQueueWaitSeconds { get; set; }
    public int SkillRangeExpansionIntervalSeconds { get; set; }
    public float InitialRatingRangeExpand { get; set; }
    public float MaxInitialRatingRangeExpand { get; set; }
    public float RatingRangeExpand { get; set; }
    public float MaxRatingRangeExpand { get; set; }
}