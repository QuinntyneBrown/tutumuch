using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Infrastructure.Photos;

/// <summary>Where <see cref="FileSystemPhotoStorage"/> keeps photos.</summary>
public sealed class PhotoStorageOptions
{
    public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "tutumuch-photos");
}

/// <summary>
/// Development adapter for <see cref="IPhotoStorage"/>: originals under "private/", versions under
/// "public/". Real object storage and the CDN replace it behind the same port.
/// </summary>
internal sealed class FileSystemPhotoStorage(IOptions<PhotoStorageOptions> options) : IPhotoStorage
{
    public Task SaveOriginalAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken) =>
        WriteAsync(PathOf("private", key), content, cancellationToken);

    public Task SavePublicAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken) =>
        WriteAsync(PathOf("public", key), content, cancellationToken);

    public Task<StoredObject?> ReadPublicAsync(string key, CancellationToken cancellationToken) =>
        ReadAsync(PathOf("public", key), cancellationToken);

    public Task<StoredObject?> ReadOriginalAsync(string key, CancellationToken cancellationToken) =>
        ReadAsync(PathOf("private", key), cancellationToken);

    public Task DeleteOriginalAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(PathOf("private", key));
        return Task.CompletedTask;
    }

    public Task DeletePublicAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(PathOf("public", key));
        return Task.CompletedTask;
    }

    private static async Task<StoredObject?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var contentType = path.EndsWith(".webp", StringComparison.Ordinal) ? "image/webp" : "image/jpeg";
        return new StoredObject(await File.ReadAllBytesAsync(path, cancellationToken), contentType);
    }

    private static async Task WriteAsync(string path, byte[] content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
    }

    /// <summary>Keys are built by TuTuMuch itself, but the path is still kept inside the root.</summary>
    private string PathOf(string area, string key)
    {
        var root = Path.GetFullPath(Path.Combine(options.Value.RootPath, area));
        var path = Path.GetFullPath(Path.Combine(root, key));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("The key leaves the storage root.", nameof(key));
        }

        return path;
    }
}
