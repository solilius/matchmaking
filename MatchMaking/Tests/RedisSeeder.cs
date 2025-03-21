using System.Text.Json;
using Matchmaking.Models;
using StackExchange.Redis;

namespace MatchMaking.Tests;

class RedisSeeder
{
    public static async Task Seed(IDatabase db )
    {
        var rand = new Random();
 
        for (int i = 1; i <= 10000; i++)
        {
            var player = new Player
            {
                Id = i.ToString(),
                Username = $"Player{i}",
                SkillRating = rand.Next(1, 5000),
                Status = 0
            };
 
            var json = JsonSerializer.Serialize(player);
            await db.StringSetAsync($"player:{player.Id}", json);
        }
        Console.WriteLine("Seeding complete!");
    }
}