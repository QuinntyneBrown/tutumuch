using System.Text;
using SkiaSharp;

namespace TutuMuch.Api.Tests.Support;

/// <summary>Builds test images in memory.</summary>
public static class Images
{
    public static byte[] Jpeg(int width, int height) => Encode(width, height, SKEncodedImageFormat.Jpeg);

    public static byte[] Png(int width, int height) => Encode(width, height, SKEncodedImageFormat.Png);

    /// <summary>A JPEG with an Exif segment carrying GPS tags and an XMP segment, as phone cameras write them.</summary>
    public static byte[] JpegWithGps(int width, int height)
    {
        var jpeg = Jpeg(width, height);
        var exif = Segment(0xE1, Concat("Exif\0\0"u8.ToArray(), Encoding.ASCII.GetBytes("MM\0*GPSLatitude=43.6532N GPSLongitude=79.3832W")));
        var xmp = Segment(0xE1, Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/\0<x:xmpmeta>GPS</x:xmpmeta>"));
        return Concat(jpeg[..2], exif, xmp, jpeg[2..]);
    }

    public static bool ContainsAscii(byte[] content, string text) =>
        content.AsSpan().IndexOf(Encoding.ASCII.GetBytes(text)) >= 0;

    private static byte[] Encode(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(new SKColor(0x2E, 0x6B, 0x8F));
        }

        using var data = bitmap.Encode(format, 90);
        return data.ToArray();
    }

    private static byte[] Segment(byte marker, byte[] payload)
    {
        var length = payload.Length + 2;
        return Concat([0xFF, marker, (byte)(length >> 8), (byte)length], payload);
    }

    private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
}
