using System.Collections.Concurrent;
using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>Keeps stored photo objects in memory in place of object storage (AGENTS.md: fake the port).</summary>
public sealed class FakePhotoStorage : IPhotoStorage
{
    public ConcurrentDictionary<string, StoredObject> Originals { get; } = new();

    public ConcurrentDictionary<string, StoredObject> Public { get; } = new();

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
        Task.FromResult(Public.TryGetValue(key, out var stored) ? stored : null);

    public Task<StoredObject?> ReadOriginalAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Originals.TryGetValue(key, out var stored) ? stored : null);

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
