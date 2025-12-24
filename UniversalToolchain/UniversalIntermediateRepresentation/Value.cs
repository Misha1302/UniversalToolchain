namespace UniversalIntermediateRepresentation;

public readonly struct Value : IEquatable<Value>
{
    public readonly object Data = null!;

    private Value(object? data)
    {
        Data = data!;
    }

    public static Value Create(object? data)
    {
        return new Value(data);
    }

    public T Get<T>()
    {
        return (T)Data;
    }

    public bool Equals(Value other)
    {
        return Equals(Data, other.Data);
    }

    public override bool Equals(object? obj)
    {
        return obj is Value other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Data);
    }

    public override string ToString()
    {
        return Data?.ToString() ?? "";
    }
}