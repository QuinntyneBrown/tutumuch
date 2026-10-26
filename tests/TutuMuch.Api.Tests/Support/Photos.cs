using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TutuMuch.Api.Tests.Support;

/// <summary>Calls for the photo upload endpoint.</summary>
public static class Photos
{
    public static Task<HttpResponseMessage> UploadPhotoAsync(this HttpClient client, byte[] content, string fileName = "photo.jpg", string contentType = "image/jpeg")
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent { { file, "file", fileName } };
        return client.PostAsync("/api/photo-uploads", form);
    }

    /// <summary>Uploads a valid 1200x1600 JPEG and returns its ID.</summary>
    public static async Task<Guid> UploadValidPhotoAsync(this HttpClient client)
    {
        using var response = await client.UploadPhotoAsync(Images.Jpeg(1200, 1600));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PhotoUpload>())!.Id;
    }
}

public sealed record PhotoUpload(Guid Id, string Url, int Width, int Height, IReadOnlyList<PhotoVersion> Versions);

public sealed record PhotoVersion(int Size, int Width, int Height, string Format, string Url);
