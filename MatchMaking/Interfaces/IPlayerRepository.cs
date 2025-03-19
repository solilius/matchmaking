using Matchmaking.Models;
using StackExchange.Redis;

namespace MatchMaking.Interfaces;

public interface IPlayerRepository
{
    Task<Player?> GetPlayerAsync(IDatabaseAsync db, string playerId);
    Task<bool> SavePlayerAsync(IDatabaseAsync db, Player player);
}