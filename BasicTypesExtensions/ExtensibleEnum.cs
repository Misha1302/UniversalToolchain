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

    public static ExtensibleEnum<TTag> CreateOrGet(string name)
    {
        return EnumGenerator.Instance<TTag>().CreateOrGet<TTag>(name);
    }

    public override string ToString()
    {
        return EnumGenerator.Instance<TTag>().GetName(_value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ExtensibleEnum<TTag>)obj);
    }

    public static bool operator ==(ExtensibleEnum<TTag>? lhs, ExtensibleEnum<TTag>? rhs)
    {
        if (lhs is null && rhs is null) return true;
        if (lhs is null || rhs is null) return false;
        return lhs.Equals(rhs);
    }

    public static bool operator !=(ExtensibleEnum<TTag> lhs, ExtensibleEnum<TTag> rhs)
    {
        return !(lhs == rhs);
    }

    public override int GetHashCode()
    {
        return _value;
    }

    public string GetName()
    {
        return EnumGenerator.Instance<TTag>().GetName(_value);
    }
}