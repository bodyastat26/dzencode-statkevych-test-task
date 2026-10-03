using System.Text.Json;
using Comments.Infrastructure.Comments;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Caching;

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

    public async Task<PagedResult<CommentDto>?> GetPageAsync(int page, CommentSort sort, CancellationToken ct)
    {
        try
        {
            var json = await cache.GetStringAsync(await BuildKeyAsync(page, sort, ct), ct);
            return json is null ? null : JsonSerializer.Deserialize<PagedResult<CommentDto>>(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Redis read failed, falling back to database");
            return null;
        }
    }

    public async Task SetPageAsync(int page, CommentSort sort, PagedResult<CommentDto> value, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(await BuildKeyAsync(page, sort, ct), json, PageTtl, ct);
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