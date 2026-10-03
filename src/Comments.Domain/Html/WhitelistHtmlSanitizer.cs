using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Comments.Domain.Validation;

namespace Comments.Domain.Html;

/// <summary>
/// Allows only &lt;a href title&gt;, &lt;code&gt;, &lt;i&gt;, &lt;strong&gt;.
/// Checks that every tag is closed in the right order and outputs valid XHTML.
/// All other text is HTML-encoded, so it can never become markup (XSS protection).
/// </summary>
public sealed partial class WhitelistHtmlSanitizer : IHtmlSanitizer
{
    private static readonly HashSet<string> AllowedTags = ["a", "code", "i", "strong"];
    private static readonly HashSet<string> AllowedLinkAttributes = ["href", "title"];

    [GeneratedRegex(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)([^<>]*)>", RegexOptions.CultureInvariant, 250)]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\G\s+([a-zA-Z][a-zA-Z0-9-]*)\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.CultureInvariant, 250)]
    private static partial Regex AttributePattern();

    public SanitizeResult Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return SanitizeResult.Fail("Text is required.");

        var output = new StringBuilder(input.Length + 32);
        var openTags = new Stack<string>();
        var position = 0;

        foreach (Match tag in TagPattern().Matches(input))
        {
            output.Append(Encode(input[position..tag.Index]));
            position = tag.Index + tag.Length;

            var isClosing = tag.Groups[1].Length > 0;
            var name = tag.Groups[2].Value.ToLowerInvariant();
            var rawAttributes = tag.Groups[3].Value;

            if (!AllowedTags.Contains(name))
                return SanitizeResult.Fail($"Tag <{name}> is not allowed. Allowed tags: <a>, <code>, <i>, <strong>.");

            if (isClosing)
            {
                if (!string.IsNullOrWhiteSpace(rawAttributes))
                    return SanitizeResult.Fail($"Closing tag </{name}> cannot have attributes.");

                if (openTags.Count == 0)
                    return SanitizeResult.Fail($"Closing tag </{name}> has no matching opening tag.");

                if (openTags.Peek() != name)
                    return SanitizeResult.Fail($"Expected </{openTags.Peek()}> but found </{name}>.");

                openTags.Pop();
                output.Append("</").Append(name).Append('>');
                continue;
            }

            if (rawAttributes.TrimEnd().EndsWith('/'))
                return SanitizeResult.Fail($"Self-closing <{name} /> is not allowed.");

            var (attributesHtml, error) = BuildAttributes(name, rawAttributes);
            if (error is not null)
                return SanitizeResult.Fail(error);

            openTags.Push(name);
            output.Append('<').Append(name).Append(attributesHtml).Append('>');
        }

        output.Append(Encode(input[position..]));

        return openTags.Count > 0
            ? SanitizeResult.Fail($"Tag <{openTags.Peek()}> is not closed.")
            : SanitizeResult.Ok(output.ToString());
    }

    private static (string Html, string? Error) BuildAttributes(string tagName, string raw)
    {
        if (tagName != "a")
            return string.IsNullOrWhiteSpace(raw)
                ? (string.Empty, null)
                : (string.Empty, $"Tag <{tagName}> cannot have attributes.");

        var values = new Dictionary<string, string>();
        var position = 0;

        while (position < raw.Length && !string.IsNullOrWhiteSpace(raw[position..]))
        {
            var match = AttributePattern().Match(raw, position);
            if (!match.Success)
                return (string.Empty, "Invalid attributes in <a>. Use: <a href=\"https://...\" title=\"...\">.");

            var attributeName = match.Groups[1].Value.ToLowerInvariant();
            if (!AllowedLinkAttributes.Contains(attributeName))
                return (string.Empty, $"Attribute '{attributeName}' is not allowed in <a>.");

            var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
            if (!values.TryAdd(attributeName, WebUtility.HtmlDecode(value)))
                return (string.Empty, $"Duplicate attribute '{attributeName}' in <a>.");

            position = match.Index + match.Length;
        }

        if (!values.TryGetValue("href", out var href) || !CommentRules.IsValidHttpUrl(href.Trim()))
            return (string.Empty, "Tag <a> requires href with a valid http(s) URL.");

        var html = new StringBuilder();
        html.Append(" href=\"").Append(Encode(href.Trim())).Append('"');
        if (values.TryGetValue("title", out var title))
            html.Append(" title=\"").Append(Encode(title)).Append('"');
        html.Append(" rel=\"nofollow noopener noreferrer\"");

        return (html.ToString(), null);
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}