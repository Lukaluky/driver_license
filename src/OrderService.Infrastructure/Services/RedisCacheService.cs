using System.Text.Json;
using OrderService.Application.Interfaces;
using StackExchange.Redis;

namespace OrderService.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(5));
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> LockAsync(string key, TimeSpan expiry)
    {
        return await _db.StringSetAsync(key, "locked", expiry, When.NotExists);
    }

    public async Task ReleaseLockAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }
}
