// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using ExceptionsManager;

namespace BasicTypesExtensions;

/// <typeparam name="TTag">Tag ('name') of your enum. LexemeTag or NodeTag, for example</typeparam>
public class ExtensibleEnum<TTag> : IEquatable<ExtensibleEnum<TTag>>
{
    private readonly int _value;

    internal ExtensibleEnum(int value)
    {
        Thrower.AssertAlways(value >= 0);
        _value = value;
    }

    public bool Equals(ExtensibleEnum<TTag>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value == other._value;
    }

    public static ExtensibleEnum<TTag> Get(string name)
    {
        return EnumGenerator.Instance<TTag>().Get<TTag>(name);
    }

    public static ExtensibleEnum<TTag> CreateNewUnique(string? name)
    {
        return EnumGenerator.Instance<TTag>().CreateNewUnique<TTag>(name);
    }

    public static ExtensibleEnum<TTag> CreateNewUniqueUnnamed()
    {
        return CreateNewUnique(null);
    }

    public override string ToString()
    {
        return EnumGenerator.Instance<TTag>().ToString(_value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ExtensibleEnum<TTag>)obj);
    }

    public override int GetHashCode()
    {
        return _value;
    }
}