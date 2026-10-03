using Comments.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Comments;

/// Decorator: adds Redis caching on top of CommentService without changing it.
public sealed class CachedCommentService(
    CommentService inner,
    CommentsCache cache,
    ILogger<CachedCommentService> logger) : ICommentService
{
    public async Task<PagedResult<CommentDto>> GetTopLevelAsync(int page, CommentSort sort, CancellationToken ct)
    {
        page = Math.Max(1, page);

        var cached = await cache.GetPageAsync(page, sort, ct);
        if (cached is not null)
        {
            logger.LogInformation("Cache HIT: page {Page}, sort {Field} {Desc}", page, sort.Field, sort.Descending);
            return cached;
        }

        logger.LogInformation("Cache MISS: page {Page}, sort {Field} {Desc}", page, sort.Field, sort.Descending);
        var result = await inner.GetTopLevelAsync(page, sort, ct);
        await cache.SetPageAsync(page, sort, result, ct);
        return result;
    }

    public async Task<CreateCommentResult> CreateAsync(CreateCommentCommand command, CancellationToken ct)
    {
        var result = await inner.CreateAsync(command, ct);
        if (result.IsSuccess)
            await cache.InvalidateAsync(ct);
        return result;
    }
}