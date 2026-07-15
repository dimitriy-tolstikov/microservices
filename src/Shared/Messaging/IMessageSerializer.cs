namespace Shared.Messaging;

public interface IMessageSerializer
{
    string Serialize<T>(T value);
    T? Deserialize<T>(string value);
}
