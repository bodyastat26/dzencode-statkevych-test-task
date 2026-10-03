namespace Comments.Domain.Html;

public sealed record SanitizeResult(bool IsValid, string Html, string? Error)
{
    public static SanitizeResult Ok(string html) => new(true, html, null);
    public static SanitizeResult Fail(string error) => new(false, string.Empty, error);
}