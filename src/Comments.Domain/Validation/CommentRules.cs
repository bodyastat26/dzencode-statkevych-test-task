using System.Text.RegularExpressions;

namespace Comments.Domain.Validation;

public static partial class CommentRules
{
    public const int UserNameMaxLength = 50;
    public const int EmailMaxLength = 254;
    public const int HomePageMaxLength = 2048;

    [GeneratedRegex("^[A-Za-z0-9]+$")]
    public static partial Regex LatinLettersAndDigits();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailFormat();

    public static bool IsValidHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <returns>Field name → error message. Empty if valid.</returns>
    public static Dictionary<string, string> Validate(string? userName, string? email, string? homePage, string? text)
    {
        userName = userName?.Trim();
        email = email?.Trim();
        homePage = homePage?.Trim();
        
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(userName))
            errors["userName"] = "User name is required.";
        else if (userName.Length > UserNameMaxLength || !LatinLettersAndDigits().IsMatch(userName))
            errors["userName"] = $"Only Latin letters and digits, up to {UserNameMaxLength} characters.";

        if (string.IsNullOrWhiteSpace(email))
            errors["email"] = "E-mail is required.";
        else if (email.Length > EmailMaxLength || !EmailFormat().IsMatch(email))
            errors["email"] = "Invalid e-mail format.";

        if (!string.IsNullOrWhiteSpace(homePage)
            && (homePage.Length > HomePageMaxLength || !IsValidHttpUrl(homePage)))
            errors["homePage"] = "Home page must be a valid http(s) URL.";

        if (string.IsNullOrWhiteSpace(text))
            errors["text"] = "Text is required.";
        else if (text.Length > Entities.Comment.MaxTextLength)
            errors["text"] = $"Text must not exceed {Entities.Comment.MaxTextLength} characters.";

        return errors;
    }
}