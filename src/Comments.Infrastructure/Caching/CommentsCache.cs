using System.Text.Json;
using Comments.Infrastructure.Comments;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Caching;

/// Result of a cache lookup: the key is computed BEFORE the database read,
/// so a concurrent invalidation can't make stale data look fresh.
public sealed record CachedPage(string? Key, PagedResult<CommentDto>? Value);

/// Caches pages of top-level comments in Redis.
/// Invalidation: every page key contains a "version"; changing the version makes all old pages stale.
/// If Redis is unavailable, the app keeps working without the cache.
public sealed class CommentsCache(IDistributedCache cache, ILogger<CommentsCache> logger)
{
    private const string VersionKey = "comments:version";

    private static readonly DistributedCacheEntryOptions PageTtl = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async Task<CachedPage> GetPageAsync(int page, CommentSort sort, CancellationToken ct)
    {
        try
        {
            var key = await BuildKeyAsync(page, sort, ct);
            var json = await cache.GetStringAsync(key, ct);
            var value = json is null ? null : JsonSerializer.Deserialize<PagedResult<CommentDto>>(json);
            return new CachedPage(key, value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Redis read failed, falling back to database");
            return new CachedPage(null, null);
        }
    }

    /// Stores the page under the key obtained BEFORE the database read.
    public async Task SetPageAsync(string key, PagedResult<CommentDto> value, CancellationToken ct)
    {
        try
        {
            await cache.SetStringAsync(key, JsonSerializer.Serialize(value), PageTtl, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Redis write failed");
        }
    }

    public async Task InvalidateAsync(CancellationToken ct = default)
    {
        try
        {
            await cache.SetStringAsync(VersionKey, Guid.NewGuid().ToString("N"), ct);
            logger.LogInformation("Comments cache invalidated");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Redis invalidation failed");
        }
    }

    private async Task<string> BuildKeyAsync(int page, CommentSort sort, CancellationToken ct)
    {
        var version = await cache.GetStringAsync(VersionKey, ct);
        if (version is null)
        {
            version = Guid.NewGuid().ToString("N");
            await cache.SetStringAsync(VersionKey, version, ct);
        }

        var direction = sort.Descending ? "desc" : "asc";
        return $"comments:{version}:page:{page}:{sort.Field}:{direction}";
    }
}