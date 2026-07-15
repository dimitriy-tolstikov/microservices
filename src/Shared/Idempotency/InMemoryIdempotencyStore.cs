using System.Collections.Concurrent;

namespace Shared.Idempotency;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<Guid, byte> _processedIds = new();

    public bool TryMarkProcessed(Guid id) => _processedIds.TryAdd(id, 0);
}
