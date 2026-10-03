using SkiaSharp;

namespace Comments.Infrastructure.Files;

public sealed record ImageFormatInfo(string Extension, string ContentType);

public sealed record ProcessedImage(byte[] Data, int Width, int Height, string Extension, string ContentType, bool Changed);

public sealed class ImageProcessor
{
    /// Detects the real format from file content. Returns null if it is not JPG/PNG/GIF.
    public ImageFormatInfo? DetectFormat(byte[] data)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(data));
        return codec?.EncodedFormat switch
        {
            SKEncodedImageFormat.Jpeg => new ImageFormatInfo(".jpg", "image/jpeg"),
            SKEncodedImageFormat.Png => new ImageFormatInfo(".png", "image/png"),
            SKEncodedImageFormat.Gif => new ImageFormatInfo(".gif", "image/gif"),
            _ => null
        };
    }

    /// Proportionally shrinks the image to fit into maxWidth x maxHeight. Smaller images stay untouched.
    public ProcessedImage FitInto(byte[] source, int maxWidth, int maxHeight)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(source))
            ?? throw new InvalidDataException("Unsupported image.");

        var format = codec.EncodedFormat;
        var width = codec.Info.Width;
        var height = codec.Info.Height;

        if (width <= maxWidth && height <= maxHeight)
        {
            var info = DetectFormat(source)!;
            return new ProcessedImage(source, width, height, info.Extension, info.ContentType, false);
        }

        var scale = Math.Min((double)maxWidth / width, (double)maxHeight / height);
        var newWidth = Math.Max(1, (int)Math.Round(width * scale));
        var newHeight = Math.Max(1, (int)Math.Round(height * scale));

        using var original = SKBitmap.Decode(codec);
        using var resized = original.Resize(new SKImageInfo(newWidth, newHeight),
            new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var image = SKImage.FromBitmap(resized);

        // Skia cannot encode GIF, so resized GIFs are saved as PNG
        var isJpeg = format == SKEncodedImageFormat.Jpeg;
        using var data = image.Encode(isJpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 90);

        return new ProcessedImage(
            data.ToArray(), newWidth, newHeight,
            isJpeg ? ".jpg" : ".png",
            isJpeg ? "image/jpeg" : "image/png",
            true);
    }
}