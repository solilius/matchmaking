using Matchmaking.Models;

namespace MatchMaking.Interfaces;

public interface IPlayerRepository
{
    Task<Player?> GetPlayerAsync(string playerId);
    Task<bool> SavePlayerAsync(Player player);
}