namespace Matchmaking.Models;

public class RedisSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public RedisKeys RedisKeys { get; set; }
}

public class RedisKeys
{
    public string PlayersQueueKey { get; set; }
    public string TasksQueueKey { get; set; }
    public string PlayersKey { get; set; }
    public string MatchesKey { get; set; }
}