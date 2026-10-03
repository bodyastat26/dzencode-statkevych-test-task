namespace Comments.Domain.Entities;

public class Comment
{
    public const int MaxTextLength = 5000;
    private const int MaxUserAgentLength = 512;

    private readonly List<Comment> _replies = [];

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public int? ParentId { get; private set; }
    public Comment? Parent { get; private set; }
    public IReadOnlyCollection<Comment> Replies => _replies;

    /// <summary>Sanitized XHTML produced by IHtmlSanitizer.</summary>
    public string Text { get; private set; } = null!;
    public string IpAddress { get; private set; } = null!;
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Attachment? Attachment { get; private set; }

    private Comment() { } // for EF Core

    public static Comment Create(User user, string sanitizedHtml, int? parentId,
        string ipAddress, string? userAgent, DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(sanitizedHtml);

        return new Comment
        {
            User = user,
            Text = sanitizedHtml,
            ParentId = parentId,
            IpAddress = ipAddress,
            UserAgent = userAgent is { Length: > MaxUserAgentLength } ? userAgent[..MaxUserAgentLength] : userAgent,
            CreatedAt = createdAtUtc
        };
    }

    public void AddAttachment(Attachment attachment)
    {
        if (Attachment is not null)
            throw new InvalidOperationException("Comment already has an attachment.");
        Attachment = attachment;
    }
}