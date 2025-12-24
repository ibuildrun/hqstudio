using System.Collections.Concurrent;

namespace HQStudio.Services
{
    /// <summary>
    /// Сервис кеширования и rate limiting для API запросов
    /// </summary>
    public class ApiCacheService
    {
        private static readonly Lazy<ApiCacheService> _instance = new(() => new ApiCacheService());
        public static ApiCacheService Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastRequestTime = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        
        // Настройки
        private readonly TimeSpan _defaultCacheDuration = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _minRequestInterval = TimeSpan.FromMilliseconds(500);
        private readonly int _maxConcurrentRequests = 3;
        
        private readonly SemaphoreSlim _globalThrottle;
        private int _pendingRequests;

        private ApiCacheService()
        {
            _globalThrottle = new SemaphoreSlim(_maxConcurrentRequests, _maxConcurrentRequests);
        }

        /// <summary>
        /// Получить данные с кешированием и rate limiting
        /// </summary>
        public async Task<T?> GetOrFetchAsync<T>(
            string cacheKey, 
            Func<Task<T>> fetchFunc, 
            TimeSpan? cacheDuration = null,
            bool forceRefresh = false) where T : class
        {
            var duration = cacheDuration ?? _defaultCacheDuration;
            
            // Проверяем кеш (если не принудительное обновление)
            if (!forceRefresh && TryGetFromCache<T>(cacheKey, out var cached))
            {
                System.Diagnostics.Debug.WriteLine($"[Cache] HIT: {cacheKey}");
                return cached;
            }

            // Rate limiting - проверяем интервал между запросами
            if (!CanMakeRequest(cacheKey))
            {
                System.Diagnostics.Debug.WriteLine($"[Cache] THROTTLED: {cacheKey}, returning stale data");
                // Возвращаем устаревшие данные если есть
                if (_cache.TryGetValue(cacheKey, out var staleEntry))
                {
                    return staleEntry.Data as T;
                }
                return null;
            }

            // Получаем или создаём lock для этого ключа
            var lockObj = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            
            // Пробуем получить lock без ожидания
            if (!await lockObj.WaitAsync(0))
            {
                System.Diagnostics.Debug.WriteLine($"[Cache] REQUEST IN PROGRESS: {cacheKey}");
                // Запрос уже выполняется, возвращаем кешированные данные
                if (_cache.TryGetValue(cacheKey, out var inProgressEntry))
                {
                    return inProgressEntry.Data as T;
                }
                // Ждём завершения текущего запроса
                await lockObj.WaitAsync();
                lockObj.Release();
                return TryGetFromCache<T>(cacheKey, out var result) ? result : null;
            }

            try
            {
                // Глобальный throttle
                Interlocked.Increment(ref _pendingRequests);
                await _globalThrottle.WaitAsync();
                
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[Cache] FETCH: {cacheKey} (pending: {_pendingRequests})");
                    
                    _lastRequestTime[cacheKey] = DateTime.UtcNow;
                    var data = await fetchFunc();
                    
                    if (data != null)
                    {
                        _cache[cacheKey] = new CacheEntry(data, DateTime.UtcNow.Add(duration));
                    }
                    
                    return data;
                }
                finally
                {
                    _globalThrottle.Release();
                    Interlocked.Decrement(ref _pendingRequests);
                }
            }
            finally
            {
                lockObj.Release();
            }
        }

        /// <summary>
        /// Инвалидировать кеш по ключу или паттерну
        /// </summary>
        public void Invalidate(string keyPattern)
        {
            var keysToRemove = _cache.Keys.Where(k => k.StartsWith(keyPattern)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
                System.Diagnostics.Debug.WriteLine($"[Cache] INVALIDATED: {key}");
            }
        }

        /// <summary>
        /// Полная очистка кеша
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _lastRequestTime.Clear();
            System.Diagnostics.Debug.WriteLine("[Cache] CLEARED");
        }

        /// <summary>
        /// Проверить, есть ли свежие данные в кеше
        /// </summary>
        public bool HasFreshData(string cacheKey)
        {
            return _cache.TryGetValue(cacheKey, out var entry) && !entry.IsExpired;
        }

        /// <summary>
        /// Количество ожидающих запросов
        /// </summary>
        public int PendingRequests => _pendingRequests;

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
}
