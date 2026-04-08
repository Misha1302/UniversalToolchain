using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Legacy;

/// <summary>
/// Parses the legacy string intrinsic names into structured symbols and type arguments.
/// </summary>
public static class LegacyIntrinsicNameParser
{
    public static bool TryParseArithmetic(
        string name,
        out IntrinsicSymbol symbol,
        out IntrinsicTypeArgument[] typeArguments)
    {
        symbol = default;
        typeArguments = [];

        var separatorIndex = name.LastIndexOf('_');
        if (separatorIndex <= 0 || separatorIndex == name.Length - 1)
            return false;

        var operation = name[..separatorIndex];
        var suffix = name[(separatorIndex + 1)..];

        if (!LegacyIntrinsicSuffixMap.TryResolveType(suffix, out var type))
            return false;

        symbol = operation switch
        {
            "add" => BuiltinIntrinsicSymbols.Arithmetic.Add,
            "sub" => BuiltinIntrinsicSymbols.Arithmetic.Subtract,
            "mul" => BuiltinIntrinsicSymbols.Arithmetic.Multiply,
            "div" => BuiltinIntrinsicSymbols.Arithmetic.Divide,
            _ => default
        };

        if (symbol == default)
            return false;

        typeArguments = [IntrinsicTypeArgument.From(type)];
        return true;
    }

    public static bool TryParseComparison(
        string name,
        out IntrinsicSymbol symbol,
        out IntrinsicTypeArgument[] typeArguments)
    {
        symbol = default;
        typeArguments = [];

        if (!name.StartsWith("cmp_", StringComparison.Ordinal))
            return false;

        var remainder = name["cmp_".Length..];
        var separatorIndex = remainder.LastIndexOf('_');
        if (separatorIndex <= 0 || separatorIndex == remainder.Length - 1)
            return false;

        var operation = remainder[..separatorIndex];
        var suffix = remainder[(separatorIndex + 1)..];

        if (!LegacyIntrinsicSuffixMap.TryResolveType(suffix, out var type))
            return false;

        symbol = operation switch
        {
            "eq" => BuiltinIntrinsicSymbols.Comparison.Equal,
            "ne" => BuiltinIntrinsicSymbols.Comparison.NotEqual,
            "gt" => BuiltinIntrinsicSymbols.Comparison.Greater,
            "ge" => BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual,
            "lt" => BuiltinIntrinsicSymbols.Comparison.Less,
            "le" => BuiltinIntrinsicSymbols.Comparison.LessOrEqual,
            _ => default
        };

        if (symbol == default)
            return false;

        typeArguments = [IntrinsicTypeArgument.From(type)];
        return true;
    }

    public static bool TryParseLoadConst(
        string name,
        out IntrinsicSymbol symbol,
        out IntrinsicTypeArgument[] typeArguments)
    {
        symbol = default;
        typeArguments = [];

        if (!name.StartsWith("load_", StringComparison.Ordinal))
            return false;

        var suffix = name["load_".Length..];
        if (!LegacyIntrinsicSuffixMap.TryResolveType(suffix, out var type))
            return false;

        symbol = BuiltinIntrinsicSymbols.Core.LoadConst;
        typeArguments = [IntrinsicTypeArgument.From(type)];
        return true;
    }
}
