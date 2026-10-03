using Comments.Domain.Html;

namespace Comments.Tests;

public class WhitelistHtmlSanitizerTests
{
    private readonly WhitelistHtmlSanitizer _sut = new();

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("<i>x</i>", "<i>x</i>")]
    [InlineData("<STRONG>x</STRONG>", "<strong>x</strong>")]
    [InlineData("<code><i>x</i></code>", "<code><i>x</i></code>")]
    [InlineData("a < b & c", "a &lt; b &amp; c")]
    public void Accepts_allowed_markup(string input, string expected)
    {
        var result = _sut.Sanitize(input);
        Assert.True(result.IsValid, result.Error);
        Assert.Equal(expected, result.Html);
    }

    [Fact]
    public void Accepts_link_with_href_and_title()
    {
        var result = _sut.Sanitize("<a href=\"https://example.com\" title=\"Example\">link</a>");
        Assert.True(result.IsValid, result.Error);
        Assert.Equal("<a href=\"https://example.com\" title=\"Example\" rel=\"nofollow noopener noreferrer\">link</a>", result.Html);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<i>not closed")]
    [InlineData("</i>")]
    [InlineData("<i><strong>x</i></strong>")]
    [InlineData("<i class=\"x\">x</i>")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
    [InlineData("<a href=\"https://x.com\" onclick=\"alert(1)\">x</a>")]
    [InlineData("<a>no href</a>")]
    public void Rejects_invalid_or_dangerous_markup(string input)
    {
        Assert.False(_sut.Sanitize(input).IsValid);
    }
}