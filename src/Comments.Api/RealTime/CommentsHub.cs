using Microsoft.AspNetCore.SignalR;

namespace Comments.Api.Realtime;

/// WebSocket hub. Clients only listen: "commentCreated" and "attachmentUpdated".
public sealed class CommentsHub : Hub
{
}