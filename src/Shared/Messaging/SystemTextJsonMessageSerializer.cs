using System.Text.Json;

namespace Shared.Messaging;

public sealed class SystemTextJsonMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    public SystemTextJsonMessageSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, _options);

    public T? Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, _options);
}
