using FluentAssertions;
using Xunit;
using System.Collections.Concurrent;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Тесты для логики кеширования (без зависимости от singleton)
/// </summary>
public class CacheServiceTests
{
    [Fact]
    public async Task GetOrFetch_FirstCall_FetchesData()
    {
        var cache = new TestCacheService();
        var fetchCount = 0;
        
        var result = await cache.GetOrFetchAsync("key1", async () =>
        {
            fetchCount++;
            await Task.Delay(10);
            return "data";
        });

        result.Should().Be("data");
        fetchCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrFetch_SecondCall_ReturnsCached()
    {
        var cache = new TestCacheService();
        var fetchCount = 0;
        
        Func<Task<string?>> fetchFunc = async () =>
        {
            fetchCount++;
            await Task.Delay(10);
            return "data";
        };

        await cache.GetOrFetchAsync("key1", fetchFunc);
        var result = await cache.GetOrFetchAsync("key1", fetchFunc);

        result.Should().Be("data");
        fetchCount.Should().Be(1); // Только один вызов
    }

    [Fact]
    public async Task GetOrFetch_ForceRefresh_FetchesAgain()
    {
        var cache = new TestCacheService();
        var fetchCount = 0;
        
        Func<Task<string?>> fetchFunc = async () =>
        {
            fetchCount++;
            await Task.Delay(10);
            return $"data{fetchCount}";
        };

        await cache.GetOrFetchAsync("key1", fetchFunc);
        var result = await cache.GetOrFetchAsync("key1", fetchFunc, forceRefresh: true);

        result.Should().Be("data2");
        fetchCount.Should().Be(2);
    }

    [Fact]
    public async Task GetOrFetch_ExpiredCache_FetchesAgain()
    {
        var cache = new TestCacheService(cacheDuration: TimeSpan.FromMilliseconds(50));
        var fetchCount = 0;
        
        Func<Task<string?>> fetchFunc = async () =>
        {
            fetchCount++;
            await Task.Delay(10);
            return $"data{fetchCount}";
        };

        await cache.GetOrFetchAsync("key1", fetchFunc);
        await Task.Delay(100); // Ждём истечения кеша
        var result = await cache.GetOrFetchAsync("key1", fetchFunc);

        result.Should().Be("data2");
        fetchCount.Should().Be(2);
    }

    [Fact]
    public void Invalidate_RemovesMatchingKeys()
    {
        var cache = new TestCacheService();
        cache.SetCache("orders_list", "data1");
        cache.SetCache("orders_123", "data2");
        cache.SetCache("clients_list", "data3");

        cache.Invalidate("orders");

        cache.HasFreshData("orders_list").Should().BeFalse();
        cache.HasFreshData("orders_123").Should().BeFalse();
        cache.HasFreshData("clients_list").Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesAllKeys()
    {
        var cache = new TestCacheService();
        cache.SetCache("key1", "data1");
        cache.SetCache("key2", "data2");

        cache.Clear();

        cache.HasFreshData("key1").Should().BeFalse();
        cache.HasFreshData("key2").Should().BeFalse();
    }

    [Fact]
    public void HasFreshData_ExpiredEntry_ReturnsFalse()
    {
        var cache = new TestCacheService();
        cache.SetCache("key1", "data", DateTime.UtcNow.AddSeconds(-10)); // Уже истёк

        cache.HasFreshData("key1").Should().BeFalse();
    }

    [Fact]
    public void HasFreshData_FreshEntry_ReturnsTrue()
    {
        var cache = new TestCacheService();
        cache.SetCache("key1", "data", DateTime.UtcNow.AddMinutes(5));

        cache.HasFreshData("key1").Should().BeTrue();
    }

    [Fact]
    public async Task GetOrFetch_NullResult_DoesNotCache()
    {
        var cache = new TestCacheService();
        var fetchCount = 0;
        
        Func<Task<string?>> fetchFunc = async () =>
        {
            fetchCount++;
            await Task.Delay(10);
            return null;
        };

        await cache.GetOrFetchAsync("key1", fetchFunc);
        await cache.GetOrFetchAsync("key1", fetchFunc);

        fetchCount.Should().Be(2); // Оба раза вызывается, т.к. null не кешируется
    }

    [Fact]
    public async Task GetOrFetch_RateLimiting_ThrottlesRequests()
    {
        var cache = new TestCacheService(minRequestInterval: TimeSpan.FromMilliseconds(100));
        var fetchCount = 0;
        
        Func<Task<string?>> fetchFunc = async () =>
        {
            fetchCount++;
            await Task.Delay(10);
            return "data";
        };

        // Первый запрос
        await cache.GetOrFetchAsync("key1", fetchFunc, forceRefresh: true);
        
        // Второй запрос сразу - должен быть throttled
        var result = await cache.GetOrFetchAsync("key1", fetchFunc, forceRefresh: true);

        // Должен вернуть кешированные данные без нового запроса
        result.Should().Be("data");
        fetchCount.Should().Be(1);
    }
}

/// <summary>
/// Тестовая реализация кеш-сервиса
/// </summary>
public class TestCacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastRequestTime = new();
    private readonly TimeSpan _defaultCacheDuration;
    private readonly TimeSpan _minRequestInterval;

    public TestCacheService(
        TimeSpan? cacheDuration = null, 
        TimeSpan? minRequestInterval = null)
    {
        _defaultCacheDuration = cacheDuration ?? TimeSpan.FromSeconds(30);
        _minRequestInterval = minRequestInterval ?? TimeSpan.FromMilliseconds(500);
    }

    public async Task<T?> GetOrFetchAsync<T>(
        string cacheKey,
        Func<Task<T?>> fetchFunc,
        TimeSpan? cacheDuration = null,
        bool forceRefresh = false) where T : class
    {
        var duration = cacheDuration ?? _defaultCacheDuration;

        // Проверяем кеш
        if (!forceRefresh && TryGetFromCache<T>(cacheKey, out var cached))
        {
            return cached;
        }

        // Rate limiting
        if (forceRefresh && !CanMakeRequest(cacheKey))
        {
            if (_cache.TryGetValue(cacheKey, out var staleEntry))
            {
                return staleEntry.Data as T;
            }
            return null;
        }

        _lastRequestTime[cacheKey] = DateTime.UtcNow;
        var data = await fetchFunc();

        if (data != null)
        {
            _cache[cacheKey] = new CacheEntry(data, DateTime.UtcNow.Add(duration));
        }

        return data;
    }

    public void SetCache(string key, object data, DateTime? expiresAt = null)
    {
        _cache[key] = new CacheEntry(data, expiresAt ?? DateTime.UtcNow.Add(_defaultCacheDuration));
    }

    public void Invalidate(string keyPattern)
    {
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(keyPattern)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void Clear()
    {
        _cache.Clear();
        _lastRequestTime.Clear();
    }

    public bool HasFreshData(string cacheKey)
    {
        return _cache.TryGetValue(cacheKey, out var entry) && !entry.IsExpired;
    }

    private bool TryGetFromCache<T>(string key, out T? value) where T : class
    {
        value = null;
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            value = entry.Data as T;
            return value != null;
        }
        return false;
    }

    private bool CanMakeRequest(string cacheKey)
    {
        if (_lastRequestTime.TryGetValue(cacheKey, out var lastTime))
        {
            var elapsed = DateTime.UtcNow - lastTime;
            if (elapsed < _minRequestInterval)
            {
                return false;
            }
        }
        return true;
    }

    private class CacheEntry
    {
        public object Data { get; }
        public DateTime ExpiresAt { get; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public CacheEntry(object data, DateTime expiresAt)
        {
            Data = data;
            ExpiresAt = expiresAt;
        }
    }
}
