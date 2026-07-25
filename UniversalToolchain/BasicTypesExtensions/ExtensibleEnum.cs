namespace BasicTypesExtensions;

/// <typeparam name="TTag">Tag ('name') of your enum. LexemeTag or NodeTag, for example.</typeparam>
public sealed class ExtensibleEnum<TTag> : IEquatable<ExtensibleEnum<TTag>>
{
    private readonly string _name;

    internal ExtensibleEnum(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Extensible enum identity must not be empty.", nameof(name));
        _name = name;
    }

    public bool Equals(ExtensibleEnum<TTag>? other) =>
        other != null && StringComparer.Ordinal.Equals(_name, other._name);

    public static ExtensibleEnum<TTag> Get(string name) => new(name);

    public static ExtensibleEnum<TTag> CreateOrGet(string name) => new(name);

    public override string ToString() => _name;

    public override bool Equals(object? obj) => obj is ExtensibleEnum<TTag> other && Equals(other);

    public static bool operator ==(ExtensibleEnum<TTag>? lhs, ExtensibleEnum<TTag>? rhs) => Equals(lhs, rhs);

    public static bool operator !=(ExtensibleEnum<TTag>? lhs, ExtensibleEnum<TTag>? rhs) => !Equals(lhs, rhs);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_name);

    public string GetName() => _name;
}
