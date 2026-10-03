namespace Comments.Domain.Html;

public interface IHtmlSanitizer
{
    SanitizeResult Sanitize(string input);
}