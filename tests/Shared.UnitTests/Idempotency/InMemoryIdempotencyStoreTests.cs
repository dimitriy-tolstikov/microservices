using FluentAssertions;
using Shared.Idempotency;

namespace Shared.UnitTests.Idempotency;

public class InMemoryIdempotencyStoreTests
{
    [Fact]
    public void TryMarkProcessed_Should_Return_True_For_New_Id()
    {
        var store = new InMemoryIdempotencyStore();
        var id = Guid.NewGuid();

        var result = store.TryMarkProcessed(id);

        result.Should().BeTrue();
    }

    [Fact]
    public void TryMarkProcessed_Should_Return_False_For_Duplicate_Id()
    {
        var store = new InMemoryIdempotencyStore();
        var id = Guid.NewGuid();

        store.TryMarkProcessed(id);
        var result = store.TryMarkProcessed(id);

        result.Should().BeFalse();
    }
}
