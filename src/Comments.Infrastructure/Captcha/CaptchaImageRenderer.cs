using SkiaSharp;

namespace Comments.Infrastructure.Captcha;

/// Draws CAPTCHA text with rotation, noise lines and dots to make OCR harder.
public sealed class CaptchaImageRenderer
{
    private const int Width = 160;
    private const int Height = 56;

    public byte[] Render(string code)
    {
        var random = Random.Shared;

        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(243, 244, 246));

        using var noisePaint = new SKPaint { IsAntialias = true, StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke };
        for (var i = 0; i < 6; i++)
        {
            noisePaint.Color = RandomColor(random, 100, 180, 140);
            canvas.DrawLine(random.Next(Width), random.Next(Height), random.Next(Width), random.Next(Height), noisePaint);
        }

        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        using var font = new SKFont(typeface, 32);
        using var textPaint = new SKPaint { IsAntialias = true };

        var step = (Width - 20f) / code.Length;
        for (var i = 0; i < code.Length; i++)
        {
            var x = 12 + i * step;
            var y = 40 + random.Next(-4, 5);

            canvas.Save();
            canvas.RotateDegrees(random.Next(-25, 26), x + 10, y - 12);
            textPaint.Color = RandomColor(random, 20, 90, 255);
            canvas.DrawText(code[i].ToString(), x, y, font, textPaint);
            canvas.Restore();
        }

        for (var i = 0; i < 80; i++)
        {
            noisePaint.Color = RandomColor(random, 80, 200, 160);
            canvas.DrawCircle(random.Next(Width), random.Next(Height), 1, noisePaint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static SKColor RandomColor(Random r, int min, int max, byte alpha) =>
        new((byte)r.Next(min, max), (byte)r.Next(min, max), (byte)r.Next(min, max), alpha);
}