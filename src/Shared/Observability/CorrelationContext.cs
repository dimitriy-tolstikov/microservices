namespace Shared.Observability;

public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _current = new();

    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public static string GetOrCreate()
    {
        if (string.IsNullOrWhiteSpace(Current))
        {
            Current = Guid.NewGuid().ToString("N");
        }

        return Current;
    }
}
