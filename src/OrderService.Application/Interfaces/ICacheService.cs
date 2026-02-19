namespace OrderService.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task<bool> LockAsync(string key, TimeSpan expiry);
    Task ReleaseLockAsync(string key);
}
