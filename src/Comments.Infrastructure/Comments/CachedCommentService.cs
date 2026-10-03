using Comments.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Comments;

/// Decorator: adds Redis caching for reads. Invalidation is done by an event handler.
public sealed class CachedCommentService(
    CommentService inner,
    CommentsCache cache,
    ILogger<CachedCommentService> logger) : ICommentService
{
    public async Task<PagedResult<CommentDto>> GetTopLevelAsync(int page, CommentSort sort, CancellationToken ct)
    {
        page = Math.Max(1, page);

        var cached = await cache.GetPageAsync(page, sort, ct);
        if (cached.Value is not null)
        {
            logger.LogInformation("Cache HIT: page {Page}, sort {Field} {Desc}", page, sort.Field, sort.Descending);
            return cached.Value;
        }

        logger.LogInformation("Cache MISS: page {Page}, sort {Field} {Desc}", page, sort.Field, sort.Descending);
        var result = await inner.GetTopLevelAsync(page, sort, ct);

        // save under the key taken before the DB read: if the cache was invalidated meanwhile,
        // this stale result goes to the old version and is never served
        if (cached.Key is not null)
            await cache.SetPageAsync(cached.Key, result, ct);

        return result;
    }

    public Task<CreateCommentResult> CreateAsync(CreateCommentCommand command, CancellationToken ct) =>
        inner.CreateAsync(command, ct);
}