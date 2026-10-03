using Comments.Infrastructure.Captcha;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/captcha")]
public class CaptchaController(ICaptchaService captcha) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var challenge = await captcha.CreateAsync(ct);
        Response.Headers.CacheControl = "no-store";
        return Ok(new
        {
            id = challenge.Id,
            image = $"data:image/png;base64,{Convert.ToBase64String(challenge.Png)}"
        });
    }
}