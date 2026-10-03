using Comments.Domain.Entities;
using Comments.Domain.Html;
using Comments.Domain.Validation;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Messaging;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Comments;

public sealed class CommentService(
    CommentsDbContext db,
    IHtmlSanitizer sanitizer,
    TimeProvider clock,
    IFileStorage storage,
    ImageProcessor images,
    IMessagePublisher publisher,
    ILogger<CommentService> logger) : ICommentService
{
    public const int PageSize = 25;
    public const long MaxImageUploadBytes = 5 * 1024 * 1024;
    private const int MaxTreeDepth = 100;

    public async Task<PagedResult<CommentDto>> GetTopLevelAsync(int page, CommentSort sort, CancellationToken ct)
    {
        page = Math.Max(1, page);

        var query = db.Comments.AsNoTracking().Where(c => c.ParentId == null);
        var totalCount = await query.CountAsync(ct);

        IOrderedQueryable<Comment> ordered = sort.Field switch
        {
            CommentSortField.UserName => sort.Descending
                ? query.OrderByDescending(c => c.User.UserName)
                : query.OrderBy(c => c.User.UserName),
            CommentSortField.Email => sort.Descending
                ? query.OrderByDescending(c => c.User.Email)
                : query.OrderBy(c => c.User.Email),
            _ => sort.Descending
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt)
        };

        var roots = await ordered
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Include(c => c.User)
            .Include(c => c.Attachment)
            .ToListAsync(ct);

        var descendants = await LoadDescendantsAsync(roots.Select(r => r.Id).ToList(), ct);
        var items = BuildTrees(roots, descendants);

        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        return new PagedResult<CommentDto>(items, page, PageSize, totalCount, totalPages);
    }

    public async Task<CreateCommentResult> CreateAsync(CreateCommentCommand command, CancellationToken ct)
    {
        var errors = CommentRules.Validate(command.UserName, command.Email, command.HomePage, command.Text);
        if (errors.Count > 0)
            return CreateCommentResult.Fail(errors);

        var sanitized = sanitizer.Sanitize(command.Text!);
        if (!sanitized.IsValid)
            return CreateCommentResult.Fail(new() { ["text"] = sanitized.Error! });

        if (command.ParentId is { } parentId && !await db.Comments.AnyAsync(c => c.Id == parentId, ct))
            return CreateCommentResult.Fail(new() { ["parentId"] = "Parent comment not found." });

        Attachment? attachment = null;
        if (command.File is not null)
        {
            var (stored, fileError) = await StoreAttachmentAsync(command.File, ct);
            if (fileError is not null)
                return CreateCommentResult.Fail(new() { ["file"] = fileError });
            attachment = stored;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var email = command.Email!.Trim().ToLowerInvariant();
        var userName = command.UserName!.Trim();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.UserName == userName, ct);
        if (user is null)
        {
            user = new User(userName, email, command.HomePage, now);
            db.Users.Add(user);
        }
        else
        {
            user.UpdateHomePage(command.HomePage);
        }

        var comment = Comment.Create(user, sanitized.Html, command.ParentId, command.IpAddress, command.UserAgent, now);
        if (attachment is not null)
            comment.AddAttachment(attachment);

        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        if (attachment is { Kind: AttachmentKind.Image })
        {
            try
            {
                await publisher.PublishAsync(Queues.ImageResize, new ImageResizeMessage(attachment.Id), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to enqueue image {Id} for resizing", attachment.Id);
            }
        }

        return CreateCommentResult.Ok(ToDto(comment));
    }

    private async Task<(Attachment? Attachment, string? Error)> StoreAttachmentAsync(FileUpload file, CancellationToken ct)
    {
        var originalName = Path.GetFileName(file.FileName);
        if (originalName.Length > 255)
            originalName = originalName[^255..];

        var extension = Path.GetExtension(originalName).ToLowerInvariant();

        if (Attachment.TextExtensions.Contains(extension))
        {
            if (file.Length > Attachment.MaxTextFileBytes)
                return (null, "Text file must not exceed 100 KB.");

            var storedName = await storage.SaveAsync(file.Content, ".txt", ct);
            return (Attachment.CreateText(originalName, storedName, file.Length), null);
        }

        if (Attachment.ImageExtensions.Contains(extension))
        {
            if (file.Length > MaxImageUploadBytes)
                return (null, "Image must not exceed 5 MB.");

            using var buffer = new MemoryStream();
            await file.Content.CopyToAsync(buffer, ct);

            // check the real content, not just the extension
            var format = images.DetectFormat(buffer.ToArray());
            if (format is null)
                return (null, "File is not a valid JPG, PNG or GIF image.");

            buffer.Position = 0;
            var storedName = await storage.SaveAsync(buffer, format.Extension, ct);
            return (Attachment.CreateImage(originalName, storedName, format.ContentType, buffer.Length), null);
        }

        return (null, "Allowed files: JPG, GIF, PNG images or a TXT file up to 100 KB.");
    }

    private async Task<List<Comment>> LoadDescendantsAsync(List<int> rootIds, CancellationToken ct)
    {
        var result = new List<Comment>();
        var frontier = rootIds;

        for (var depth = 0; frontier.Count > 0 && depth < MaxTreeDepth; depth++)
        {
            var currentLevel = frontier;
            var children = await db.Comments.AsNoTracking()
                .Where(c => c.ParentId != null && currentLevel.Contains(c.ParentId.Value))
                .Include(c => c.User)
                .Include(c => c.Attachment)
                .ToListAsync(ct);

            result.AddRange(children);
            frontier = children.Select(c => c.Id).ToList();
        }

        return result;
    }

    private static List<CommentDto> BuildTrees(List<Comment> roots, List<Comment> descendants)
    {
        var all = roots.Concat(descendants).ToDictionary(c => c.Id, ToDto);

        foreach (var child in descendants.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id))
        {
            if (child.ParentId is { } pid && all.TryGetValue(pid, out var parent))
                parent.Replies.Add(all[child.Id]);
        }

        return roots.Select(r => all[r.Id]).ToList();
    }

    private static CommentDto ToDto(Comment c) => new(
        c.Id,
        c.ParentId,
        c.User.UserName,
        c.User.Email,
        c.User.HomePage,
        c.Text,
        c.CreatedAt,
        c.Attachment is null ? null : new AttachmentDto(
            c.Attachment.Id,
            c.Attachment.Kind.ToString(),
            c.Attachment.Status.ToString(),
            c.Attachment.OriginalFileName,
            $"/uploads/{c.Attachment.StoredFileName}",
            c.Attachment.Width,
            c.Attachment.Height),
        []);
}