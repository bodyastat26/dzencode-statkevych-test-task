namespace Comments.Infrastructure.Comments;

public sealed record AttachmentDto(
    int Id, string Kind, string Status, string OriginalFileName, string Url, int? Width, int? Height);

public sealed record CommentDto(
    int Id,
    int? ParentId,
    string UserName,
    string Email,
    string? HomePage,
    string Text,
    DateTime CreatedAt,
    AttachmentDto? Attachment,
    List<CommentDto> Replies);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record CreateCommentCommand(
    string? UserName,
    string? Email,
    string? HomePage,
    string? Text,
    int? ParentId,
    string IpAddress,
    string? UserAgent);

public sealed record CreateCommentResult(CommentDto? Comment, Dictionary<string, string>? Errors)
{
    public bool IsSuccess => Errors is null;
    public static CreateCommentResult Ok(CommentDto comment) => new(comment, null);
    public static CreateCommentResult Fail(Dictionary<string, string> errors) => new(null, errors);
}