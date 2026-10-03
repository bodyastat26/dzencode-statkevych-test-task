namespace Comments.Infrastructure.Captcha;

public sealed record CaptchaChallenge(string Id, byte[] Png);

public interface ICaptchaService
{
    Task<CaptchaChallenge> CreateAsync(CancellationToken ct);
    Task<bool> ValidateAsync(string? id, string? code, CancellationToken ct);
}