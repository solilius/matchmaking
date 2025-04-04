using Matchmaking.Models;
using StackExchange.Redis;

namespace MatchMaking.Interfaces;

public interface IRepository<T>
{
    Task<T?> GetAsync(IDatabaseAsync db, string id);
    Task<string?> GetJsonAsync(IDatabaseAsync db, string id);
    void EnqueueSaveAsync(ITransaction db, T entity);
    Task<bool> SaveAsync(IDatabaseAsync db, T entity);
    void EnqueueRemoveAsync(ITransaction db, string id);

    string GetKey(string id);
}