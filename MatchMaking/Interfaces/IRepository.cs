using Matchmaking.Models;
using StackExchange.Redis;

namespace MatchMaking.Interfaces;

public interface IRepository<T>
{
    Task<T?> GetAsync(IDatabaseAsync db, string id);
    Task<string?> GetJsonAsync(IDatabaseAsync db, string id);
    Task<bool> SaveAsync(IDatabaseAsync db, T entity);
    
    string GetKey(string id);
}