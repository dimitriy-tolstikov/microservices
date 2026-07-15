using FluentAssertions;
using Shared.Messaging;

namespace Shared.UnitTests.Messaging;

public class SystemTextJsonMessageSerializerTests
{
    [Fact]
    public void Serialize_And_Deserialize_Should_Preserve_Data()
    {
        var serializer = new SystemTextJsonMessageSerializer();
        var message = new TestMessage { Id = 42, Name = "Test" };

        var json = serializer.Serialize(message);
        var deserialized = serializer.Deserialize<TestMessage>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(message.Id);
        deserialized.Name.Should().Be(message.Name);
    }

    private sealed class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
