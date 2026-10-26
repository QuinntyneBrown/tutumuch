using System.Collections.Concurrent;
using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Cli.Tests.Support;

/// <summary>In-memory <see cref="IPhotoStorage"/>; no folder or object store is touched.</summary>
public sealed class FakePhotoStorage : IPhotoStorage
{
    public ConcurrentDictionary<string, StoredObject> Public { get; } = new();

    public ConcurrentDictionary<string, StoredObject> Originals { get; } = new();

    public Task SaveOriginalAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        Originals[key] = new StoredObject(content, contentType);
        return Task.CompletedTask;
    }

    public Task SavePublicAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        Public[key] = new StoredObject(content, contentType);
        return Task.CompletedTask;
    }

    public Task<StoredObject?> ReadPublicAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Public.GetValueOrDefault(key));

    public Task<StoredObject?> ReadOriginalAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Originals.GetValueOrDefault(key));

    public Task DeleteOriginalAsync(string key, CancellationToken cancellationToken)
    {
        Originals.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task DeletePublicAsync(string key, CancellationToken cancellationToken)
    {
        Public.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
