namespace Comments.Api.Contracts;

public sealed class CreateCommentRequest
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? HomePage { get; set; }
    public string? Text { get; set; }
    public int? ParentId { get; set; }
}

public sealed record PreviewRequest(string? Text);