using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Distributed;

namespace Comments.Infrastructure.Captcha;

public sealed class CaptchaService(IDistributedCache cache, CaptchaImageRenderer renderer) : ICaptchaService
{
    // Latin letters and digits without look-alikes (0/O, 1/I/L)
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 5;

    private static readonly DistributedCacheEntryOptions Ttl = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async Task<CaptchaChallenge> CreateAsync(CancellationToken ct)
    {
        var code = RandomNumberGenerator.GetString(Alphabet, CodeLength);
        var id = Guid.NewGuid().ToString("N");

        await cache.SetStringAsync(Key(id), code, Ttl, ct);
        return new CaptchaChallenge(id, renderer.Render(code));
    }

    public async Task<bool> ValidateAsync(string? id, string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64 || string.IsNullOrWhiteSpace(code))
            return false;

        var expected = await cache.GetStringAsync(Key(id), ct);
        if (expected is null)
            return false;

        await cache.RemoveAsync(Key(id), ct); // one-time use: the same CAPTCHA can't be reused
        return string.Equals(expected, code.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string Key(string id) => $"captcha:{id}";
}