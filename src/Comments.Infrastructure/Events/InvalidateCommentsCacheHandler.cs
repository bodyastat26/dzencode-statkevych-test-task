using Comments.Infrastructure.Caching;

namespace Comments.Infrastructure.Events;

public sealed class InvalidateCommentsCacheHandler(CommentsCache cache) :
    IEventHandler<CommentCreatedEvent>,
    IEventHandler<AttachmentProcessedEvent>
{
    public Task HandleAsync(CommentCreatedEvent @event, CancellationToken ct) => cache.InvalidateAsync(ct);

    public Task HandleAsync(AttachmentProcessedEvent @event, CancellationToken ct) => cache.InvalidateAsync(ct);
}