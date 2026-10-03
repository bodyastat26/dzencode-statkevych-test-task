using Comments.Infrastructure.Comments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Events;

public interface IEvent { }

public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}

public interface IEventDispatcher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : IEvent;
}

public sealed record CommentCreatedEvent(CommentDto Comment) : IEvent;

public sealed record AttachmentProcessedEvent(int CommentId, AttachmentDto Attachment) : IEvent;

/// Calls every registered handler for the event. One failing handler doesn't stop the others.
public sealed class EventDispatcher(IServiceProvider services, ILogger<EventDispatcher> logger) : IEventDispatcher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : IEvent
    {
        foreach (var handler in services.GetServices<IEventHandler<TEvent>>())
        {
            try
            {
                await handler.HandleAsync(@event, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Handler {Handler} failed for {Event}", handler.GetType().Name, typeof(TEvent).Name);
            }
        }
    }
}