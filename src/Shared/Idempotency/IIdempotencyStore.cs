namespace Shared.Idempotency;

public interface IIdempotencyStore
{
    bool TryMarkProcessed(Guid id);
}
