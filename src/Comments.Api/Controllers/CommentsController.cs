using Comments.Api.Contracts;
using Comments.Domain.Html;
using Comments.Infrastructure.Comments;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController(ICommentService comments, IHtmlSanitizer sanitizer) : ControllerBase
{
    /// Top-level comments with full reply trees. 25 per page, default sort: newest first.
    [HttpGet]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetTopLevel(
        [FromQuery] int page = 1,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortDir = "desc",
        CancellationToken ct = default)
    {
        var result = await comments.GetTopLevelAsync(page, CommentSort.Parse(sortBy, sortDir), ct);
        return Ok(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateCommentRequest request, CancellationToken ct)
    {
        var command = new CreateCommentCommand(
            request.UserName,
            request.Email,
            request.HomePage,
            request.Text,
            request.ParentId,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString());

        var result = await comments.CreateAsync(command, ct);
        if (!result.IsSuccess)
            return ToValidationProblem(result.Errors!);

        return Created($"/api/comments/{result.Comment!.Id}", result.Comment);
    }

    /// Server-side preview: returns the sanitized HTML without saving anything.
    [HttpPost("preview")]
    public IActionResult Preview([FromBody] PreviewRequest request)
    {
        var result = sanitizer.Sanitize(request.Text ?? string.Empty);
        return result.IsValid
            ? Ok(new { html = result.Html })
            : ToValidationProblem(new() { ["text"] = result.Error! });
    }

    private IActionResult ToValidationProblem(Dictionary<string, string> errors) =>
        ValidationProblem(new ValidationProblemDetails(
            errors.ToDictionary(e => e.Key, e => new[] { e.Value })));
}