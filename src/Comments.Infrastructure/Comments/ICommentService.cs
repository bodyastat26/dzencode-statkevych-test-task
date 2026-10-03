namespace Comments.Infrastructure.Comments;

public interface ICommentService
{
    Task<PagedResult<CommentDto>> GetTopLevelAsync(int page, CommentSort sort, CancellationToken ct);
    Task<CreateCommentResult> CreateAsync(CreateCommentCommand command, CancellationToken ct);
}