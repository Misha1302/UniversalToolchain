// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using ExceptionsManager;

namespace BasicLexer;

public class LexemeType : IEquatable<LexemeType>
{
    private static int _num;
    private static readonly List<string?> _names = [];

    public readonly int Value;

    private LexemeType(int value)
    {
        Thrower.AssertAlways(value >= 0);
        Value = value;
    }

    public bool Equals(LexemeType? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public static LexemeType Get(string name)
    {
        var index = _names.IndexOf(name);
        Thrower.AssertAlways(index >= 0);
        return new LexemeType(index);
    }

    public static LexemeType CreateNewUnique(string? name)
    {
        _names.Add(name);
        return new LexemeType(_num++);
    }

    public static LexemeType CreateNewUniqueUnnamed()
    {
        return CreateNewUnique(null);
    }

    public override string ToString()
    {
        return _names[Value] ?? Value.ToString();
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((LexemeType)obj);
    }

    public override int GetHashCode()
    {
        return Value;
    }
}