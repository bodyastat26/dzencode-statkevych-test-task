namespace Comments.Infrastructure.Messaging;

public static class Queues
{
    public const string ImageResize = "image-resize";
}

public sealed record ImageResizeMessage(int AttachmentId);