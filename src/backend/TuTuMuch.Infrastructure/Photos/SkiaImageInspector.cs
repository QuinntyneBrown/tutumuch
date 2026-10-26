using SkiaSharp;
using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Infrastructure.Photos;

/// <summary>
/// Identifies JPEG, PNG and WebP from their magic bytes and reads the dimensions from the header
/// without decoding the pixels. Dimensions are as displayed, after the EXIF orientation.
/// </summary>
internal sealed class SkiaImageInspector : IImageInspector
{
    public ImageInfo? Inspect(byte[] content)
    {
        if (Sniff(content) is not { } contentType)
        {
            return null;
        }

        using var data = SKData.CreateCopy(content);
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0)
        {
            return null;
        }

        var (width, height) = SkiaPhotoProcessor.SwapsAxes(codec.EncodedOrigin)
            ? (codec.Info.Height, codec.Info.Width)
            : (codec.Info.Width, codec.Info.Height);
        return new ImageInfo(contentType, width, height);
    }

    /// <summary>FF D8 FF is JPEG, 89 50 4E 47 0D 0A 1A 0A is PNG, RIFF....WEBP is WebP.</summary>
    internal static string? Sniff(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(JpegMagic))
        {
            return "image/jpeg";
        }

        if (content.StartsWith(PngMagic))
        {
            return "image/png";
        }

        if (content.Length >= 12 && content[..4].SequenceEqual("RIFF"u8) && content[8..12].SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return null;
    }

    private static ReadOnlySpan<byte> JpegMagic => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> PngMagic => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
}
