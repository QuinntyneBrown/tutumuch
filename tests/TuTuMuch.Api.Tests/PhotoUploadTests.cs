// Acceptance Test
// Traces to: L2-010, L2-017, L2-059
// Description: A verified member uploads one photo. The server checks size, the type from the
// content and the shortest side, strips all metadata, and stores 400, 800 and 1600 px versions in
// WebP and JPEG under content-addressed keys served with a one-year cache header.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;

namespace TuTuMuch.Api.Tests;

public sealed class PhotoUploadTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _client;

    public PhotoUploadTests()
    {
        _client = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task A_visitor_cannot_upload()
    {
        using var response = await _client.UploadPhotoAsync(Images.Jpeg(1200, 1600));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_valid_jpeg_is_accepted_with_three_sizes_in_two_formats()
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        using var response = await _client.UploadPhotoAsync(Images.Jpeg(1200, 1600));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var photo = (await response.Content.ReadFromJsonAsync<PhotoUpload>())!;
        Assert.Equal($"/api/photo-uploads/{photo.Id}", response.Headers.Location!.OriginalString);
        Assert.Equal((1200, 1600), (photo.Width, photo.Height));
        Assert.Equal(
            ["400 WebP 400x533", "400 Jpeg 400x533", "800 WebP 800x1067", "800 Jpeg 800x1067", "1600 WebP 1200x1600", "1600 Jpeg 1200x1600"],
            photo.Versions.Select(v => $"{v.Size} {v.Format} {v.Width}x{v.Height}"));
        Assert.Matches("^/photos/[0-9a-f]{64}/400\\.webp$", photo.Url);
        Assert.Equal(6, _factory.Photos.Public.Count);
        Assert.Single(_factory.Photos.Originals);
    }

    [Fact]
    public async Task Versions_are_served_with_a_one_year_immutable_cache_header()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        using var upload = await _client.UploadPhotoAsync(Images.Png(1000, 800), "front.png", "image/png");
        var photo = (await upload.Content.ReadFromJsonAsync<PhotoUpload>())!;
        using var visitor = _factory.CreateMemberClient();

        using var response = await visitor.GetAsync(photo.Versions.Single(v => v.Size == 800 && v.Format == "Jpeg").Url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task An_11_mb_file_is_refused_with_photos_must_be_10_mb_or_smaller()
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        using var response = await _client.UploadPhotoAsync(new byte[11 * 1024 * 1024]);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("PHOTO_TOO_LARGE", problem.Code);
        Assert.Equal("Photos must be 10 MB or smaller", problem.Title);
    }

    [Fact]
    public async Task A_600_by_900_image_is_refused_with_a_minimum_size_error()
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        using var response = await _client.UploadPhotoAsync(Images.Jpeg(600, 900));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("PHOTO_TOO_SMALL", problem.Code);
        Assert.Equal("Photos must be at least 800 pixels on the shortest side", problem.Title);
    }

    [Fact]
    public async Task A_text_file_named_photo_jpg_is_refused_with_415()
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        using var response = await _client.UploadPhotoAsync("not an image at all"u8.ToArray(), "photo.jpg", "image/jpeg");

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("PHOTO_TYPE_UNSUPPORTED", problem.Code);
        Assert.Equal("Use a JPEG, PNG or WebP photo", problem.Title);
        Assert.Empty(_factory.Photos.Public);
    }

    [Fact]
    public async Task No_stored_version_keeps_exif_xmp_or_gps_metadata()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var original = Images.JpegWithGps(1200, 1600);
        Assert.True(Images.ContainsAscii(original, "GPSLatitude"));

        using var response = await _client.UploadPhotoAsync(original);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.All(_factory.Photos.Public.Values, stored =>
        {
            Assert.False(Images.ContainsAscii(stored.Content, "Exif"));
            Assert.False(Images.ContainsAscii(stored.Content, "GPS"));
            Assert.False(Images.ContainsAscii(stored.Content, "xmpmeta"));
        });
    }

    [Fact]
    public async Task Another_members_photo_answers_404()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var id = await _client.UploadValidPhotoAsync();
        using var other = _factory.CreateMemberClient();
        await other.SignInVerifiedMemberAsync(_factory, "other_seller", "other.seller@example.com");

        using var mine = await _client.GetAsync($"/api/photo-uploads/{id}");
        using var theirs = await other.GetAsync($"/api/photo-uploads/{id}");

        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, theirs.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
