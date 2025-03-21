using System.Text.Json;
using Matchmaking.Models;
using StackExchange.Redis;

namespace MatchMaking.Tests;

class RedisSeeder
{
    private static readonly ConnectionMultiplexer _redis = ConnectionMultiplexer.Connect("localhost:6379"); // change if needed
    private static readonly IDatabase _db = _redis.GetDatabase();
 
    public static async Task Seed()
    {
        var rand = new Random();
 
        for (int i = 1; i <= 10000; i++)
        {
            var player = new Player
            {
                Id = i.ToString(),
                Username = $"Player{i}",
                SkillRating = rand.Next(1, 2000),
                Status = 0
            };
 
            var json = JsonSerializer.Serialize(player);
            await _db.StringSetAsync($"player:{player.Id}", json);
        }
        Console.WriteLine("Seeding complete!");
    }
}