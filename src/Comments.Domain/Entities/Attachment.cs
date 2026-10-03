namespace Comments.Domain.Entities;

public enum AttachmentKind { Image = 1, Text = 2 }
public enum AttachmentStatus { Processing = 1, Ready = 2, Failed = 3 }

public class Attachment
{
    public const int MaxImageWidth = 320;
    public const int MaxImageHeight = 240;
    public const long MaxTextFileBytes = 100 * 1024;
    public static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".gif"];
    public static readonly string[] TextExtensions = [".txt"];

    public int Id { get; private set; }
    public int CommentId { get; private set; }
    public Comment Comment { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string StoredFileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public AttachmentKind Kind { get; private set; }
    public AttachmentStatus Status { get; private set; }
    public long SizeBytes { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }

    private Attachment() { } // for EF Core

    public static Attachment CreateImage(string originalFileName, string storedFileName, string contentType, long sizeBytes) =>
        new()
        {
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Kind = AttachmentKind.Image,
            Status = AttachmentStatus.Processing // resized later by a queue consumer
        };

    public static Attachment CreateText(string originalFileName, string storedFileName, long sizeBytes)
    {
        if (sizeBytes > MaxTextFileBytes)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Text file must not exceed 100 KB.");

        return new()
        {
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            ContentType = "text/plain",
            SizeBytes = sizeBytes,
            Kind = AttachmentKind.Text,
            Status = AttachmentStatus.Ready
        };
    }

    public void MarkProcessed(int width, int height, long sizeBytes, string storedFileName, string contentType)
    {
        Width = width;
        Height = height;
        SizeBytes = sizeBytes;
        StoredFileName = storedFileName;
        ContentType = contentType;
        Status = AttachmentStatus.Ready;
    }

    public void MarkFailed() => Status = AttachmentStatus.Failed;
}