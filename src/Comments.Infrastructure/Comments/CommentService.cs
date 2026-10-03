using Comments.Domain.Entities;
using Comments.Domain.Html;
using Comments.Domain.Validation;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Comments.Infrastructure.Comments;

public sealed class CommentService(CommentsDbContext db, IHtmlSanitizer sanitizer, TimeProvider clock) : ICommentService
{
    public const int PageSize = 25;
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
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        return CreateCommentResult.Ok(ToDto(comment));
    }

    /// Loads all replies of the given roots level by level (one query per depth level).
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

        // replies are shown oldest first, like a conversation
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