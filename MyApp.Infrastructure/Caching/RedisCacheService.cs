using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyApp.Application.Interfaces;
using StackExchange.Redis;

namespace MyApp.Infrastructure.Caching;

/// <summary>
/// Redis-backed cache service using StackExchange.Redis.
/// Serializes values as JSON. All operations are resilient — failures are logged, not thrown.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _db     = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            var json = (string?)value;
            if (json is null) return default;
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOpts);
            await _db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await _db.KeyDeleteAsync(key); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis DEL failed for key {Key}", key); }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try { return await _db.KeyExistsAsync(key); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis EXISTS check failed for key {Key}", key);
            return false;
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        try
        {
            var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
            var keys   = server.Keys(pattern: $"{prefix}*").ToArray();
            if (keys.Length > 0) await _db.KeyDeleteAsync(keys);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis prefix delete failed for prefix {Prefix}", prefix);
        }
    }
}
