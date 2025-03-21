namespace Matchmaking.Models;

public class RedisSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public RedisKeys RedisKeys { get; set; }
    public int MatchTTL { get; set; }
    public int LobbyTTL { get; set; }
}

public class RedisKeys
{
    public string LobbyKey { get; set; }
    public string QueueKey { get; set; }
    public string PlayersKey { get; set; }
    public string MatchesKey { get; set; }
    public string PlayerMatchKey { get; set; }
}