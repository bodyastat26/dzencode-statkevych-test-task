using Comments.Infrastructure.Events;
using Microsoft.AspNetCore.SignalR;

namespace Comments.Api.Realtime;

/// Event handler that pushes changes to all connected browsers over WebSocket.
public sealed class SignalRBroadcastHandler(
    IHubContext<CommentsHub> hub,
    ILogger<SignalRBroadcastHandler> logger) :
    IEventHandler<CommentCreatedEvent>,
    IEventHandler<AttachmentProcessedEvent>
{
    public async Task HandleAsync(CommentCreatedEvent @event, CancellationToken ct)
    {
        await hub.Clients.All.SendAsync("commentCreated", @event.Comment, ct);
        logger.LogInformation("WebSocket: commentCreated {Id} sent", @event.Comment.Id);
    }

    public async Task HandleAsync(AttachmentProcessedEvent @event, CancellationToken ct)
    {
        await hub.Clients.All.SendAsync("attachmentUpdated", new { @event.CommentId, @event.Attachment }, ct);
        logger.LogInformation("WebSocket: attachmentUpdated for comment {Id} sent", @event.CommentId);
    }
}