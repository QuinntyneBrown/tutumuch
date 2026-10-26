using SkiaSharp;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Infrastructure.Photos;

/// <summary>
/// Decodes the original into sRGB pixels, applies the EXIF orientation and re-encodes each width.
/// Only pixels are encoded, so no EXIF, XMP, IPTC, GPS or ICC block survives (L2-017). Settings
/// are fixed, so the same original always gives the same bytes under the same key (L2-059).
/// </summary>
internal sealed class SkiaPhotoProcessor : IListingPhotoProcessor
{
    public const int WebPQuality = 80;
    public const int JpegQuality = 82;

    public IReadOnlyList<ProcessedPhotoVersion> Process(byte[] original, IReadOnlyList<int> sizes)
    {
        using var data = SKData.CreateCopy(original);
        using var codec = SKCodec.Create(data) ?? throw new InvalidOperationException("The photo cannot be decoded.");
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());
        using var decoded = new SKBitmap(info);
        var result = codec.GetPixels(info, decoded.GetPixels());
        if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
        {
            throw new InvalidOperationException($"The photo cannot be decoded: {result}.");
        }

        using var upright = Orient(decoded, codec.EncodedOrigin);
        var versions = new List<ProcessedPhotoVersion>();
        foreach (var size in sizes.Order())
        {
            var width = Math.Min(size, upright.Width);
            var height = (int)Math.Round((double)upright.Height * width / upright.Width, MidpointRounding.AwayFromZero);
            using var opaque = Render(upright, width, height);
            versions.Add(new(size, PhotoFormat.WebP, width, height, Encode(opaque, SKEncodedImageFormat.Webp, WebPQuality)));
            versions.Add(new(size, PhotoFormat.Jpeg, width, height, Encode(opaque, SKEncodedImageFormat.Jpeg, JpegQuality)));
        }

        return versions;
    }

    internal static bool SwapsAxes(SKEncodedOrigin origin) =>
        origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

    private static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format, int quality)
    {
        using var data = bitmap.Encode(format, quality) ?? throw new InvalidOperationException($"The photo cannot be encoded as {format}.");
        return data.ToArray();
    }

    /// <summary>
    /// Scales onto an opaque white bitmap without a colour space, so the encoders write no ICC
    /// profile and transparent PNG areas become white.
    /// </summary>
    private static SKBitmap Render(SKBitmap source, int width, int height)
    {
        var target = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using var canvas = new SKCanvas(target);
        using var image = SKImage.FromBitmap(source);
        canvas.Clear(SKColors.White);
        canvas.DrawImage(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        return target;
    }

    /// <summary>Redraws the pixels so the photo stands upright; the orientation tag is dropped with the rest.</summary>
    private static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return source.Copy();
        }

        var swap = SwapsAxes(origin);
        var target = new SKBitmap(new SKImageInfo(swap ? source.Height : source.Width, swap ? source.Width : source.Height, source.ColorType, source.AlphaType, source.ColorSpace));
        using var canvas = new SKCanvas(target);
        canvas.SetMatrix(OriginMatrix(origin, source.Width, source.Height));
        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, 0, 0, SKSamplingOptions.Default);
        return target;
    }

    /// <summary>Maps source pixels to the upright image for each EXIF orientation.</summary>
    private static SKMatrix OriginMatrix(SKEncodedOrigin origin, float w, float h) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
        _ => SKMatrix.Identity,
    };
}
