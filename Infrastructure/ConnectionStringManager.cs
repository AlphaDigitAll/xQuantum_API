using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using xQuantum_API.Interfaces;

namespace xQuantum_API.Infrastructure
{
    public class ConnectionStringManager : IConnectionStringManager
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<ConnectionStringManager> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);

        public ConnectionStringManager(IMemoryCache cache,ILogger<ConnectionStringManager> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        /// <summary>
        /// Get or add connection string with thread-safe mechanism
        /// </summary>
        public async Task<string> GetOrAddConnectionStringAsync(string orgId,Func<Task<string>> connectionStringFactory)
        {
            if (string.IsNullOrWhiteSpace(orgId))
                throw new ArgumentException("OrgId cannot be null or empty", nameof(orgId));

            // Try to get from cache first
            if (_cache.TryGetValue(orgId, out string cachedConnectionString))
            {
                _logger.LogDebug("Connection string retrieved from cache for tenant: {OrgId}", orgId);
                return cachedConnectionString;
            }

            // Get or create a lock for this tenant
            var lockObj = _locks.GetOrAdd(orgId, _ => new SemaphoreSlim(1, 1));

            await lockObj.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue(orgId, out cachedConnectionString))
                {
                    _logger.LogDebug("Connection string retrieved from cache after lock for tenant: {OrgId}", orgId);
                    return cachedConnectionString;
                }

                // Fetch connection string
                _logger.LogInformation("Fetching new connection string for tenant: {OrgId}", orgId);
                var connectionString = await connectionStringFactory();

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException($"Connection string factory returned null or empty for tenant: {orgId}");
                }

                // Store in cache with sliding expiration
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = _cacheExpiration,
                    Priority = CacheItemPriority.High
                }.SetSize(1);

                _cache.Set(orgId, connectionString, cacheOptions);
                _logger.LogInformation("Connection string cached for tenant: {OrgId}", orgId);

                return connectionString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connection string for tenant: {OrgId}", orgId);
                throw;
            }
            finally
            {
                lockObj.Release();
            }
        }

        /// <summary>
        /// Remove connection string from cache
        /// </summary>
        public void RemoveConnectionString(string orgId)
        {
            if (string.IsNullOrWhiteSpace(orgId))
                return;

            _cache.Remove(orgId);
            _locks.TryRemove(orgId, out _);
            _logger.LogInformation("Connection string removed from cache for tenant: {OrgId}", orgId);
        }

        /// <summary>
        /// Clear all cached connection strings
        /// </summary>
        public void ClearAll()
        {
            _locks.Clear();
            _logger.LogWarning("All connection strings cleared from cache");
        }
    }
}