using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ShopElegance.Infrastructure.Caching;

// ─── Interface ────────────────────────────────────────────────────────────────
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class;
}

// ─── Memory Cache Implementation ─────────────────────────────────────────────
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(15);

    // Track all keys so we can remove by prefix
    private static readonly HashSet<string> _keys = new();
    private static readonly object _lock = new();

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(expiry ?? DefaultExpiry)
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .RegisterPostEvictionCallback((k, _, _, _) =>
            {
                lock (_lock) { _keys.Remove(k.ToString()!); }
            });

        _cache.Set(key, value, options);
        lock (_lock) { _keys.Add(key); }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        lock (_lock) { _keys.Remove(key); }
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        List<string> toRemove;
        lock (_lock) { toRemove = _keys.Where(k => k.StartsWith(prefix)).ToList(); }
        foreach (var key in toRemove)
        {
            _cache.Remove(key);
            lock (_lock) { _keys.Remove(key); }
        }
        _logger.LogDebug("Removed {Count} cache entries with prefix '{Prefix}'", toRemove.Count, prefix);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null) return cached;

        var value = await factory();
        await SetAsync(key, value, expiry);
        return value;
    }
}

// ─── Redis / Distributed Cache Implementation ─────────────────────────────────
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(30);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var bytes = await _cache.GetAsync(key);
            if (bytes == null) return null;
            return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache GET failed for key '{Key}'", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(expiry ?? DefaultExpiry)
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));
            await _cache.SetAsync(key, bytes, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache SET failed for key '{Key}'", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try { await _cache.RemoveAsync(key); }
        catch (Exception ex) { _logger.LogError(ex, "Cache REMOVE failed for key '{Key}'", key); }
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        // Redis SCAN-based removal requires StackExchange.Redis directly.
        // For now log a warning; hook StackExchange.Redis server commands here for production.
        _logger.LogWarning("RemoveByPrefix '{Prefix}' requires direct Redis server connection for bulk delete", prefix);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null) return cached;

        var value = await factory();
        await SetAsync(key, value, expiry);
        return value;
    }
}

// ─── Cache Keys — centralised constants ──────────────────────────────────────
public static class CacheKeys
{
    public static string FeaturedProducts(int count) => $"products:featured:{count}";
    public static string BestSellers(int count)      => $"products:bestsellers:{count}";
    public static string ProductBySlug(string slug)  => $"products:slug:{slug}";
    public static string ProductById(int id)         => $"products:id:{id}";
    public static string CategoryList()              => "categories:all";
    public static string ActiveCategories()          => "categories:active";
    public static string CartCount(string userId)    => $"cart:count:{userId}";
    public static string DashboardStats()            => "dashboard:stats";

    public const string ProductsPrefix    = "products:";
    public const string CategoriesPrefix  = "categories:";
    public const string DashboardPrefix   = "dashboard:";
}
