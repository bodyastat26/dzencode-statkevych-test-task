namespace Comments.Api.Contracts;
using Comments.Domain.Validation;
using Comments.Infrastructure.Captcha;

public sealed class CreateCommentRequest
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? HomePage { get; set; }
    public string? Text { get; set; }
    public int? ParentId { get; set; }
    public string? CaptchaId { get; set; }
    public string? CaptchaCode { get; set; }
}

public sealed record PreviewRequest(string? Text);