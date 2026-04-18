using BasicCore.Builtins;
using BasicCore.Legacy;

namespace BasicCore.Capabilities;

public static class LegacyCapabilityNameEncoder
{
    public static bool TryEncode(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        out string capabilityName)
    {
        if (TryEncodeTypedOperation(symbol, typeArguments, out capabilityName))
            return true;

        capabilityName = symbol switch
        {
            _ when symbol == BuiltinIntrinsicSymbols.Boolean.And => "boolean_and",
            _ when symbol == BuiltinIntrinsicSymbols.Boolean.Or => "boolean_or",
            _ when symbol == BuiltinIntrinsicSymbols.Boolean.Not => "boolean_not",
            _ when symbol == BuiltinIntrinsicSymbols.Storage.LoadLocal => "load_local",
            _ when symbol == BuiltinIntrinsicSymbols.Storage.StoreLocal => "store_local",
            _ when symbol == BuiltinIntrinsicSymbols.Storage.LoadLocalRef => "load_local_ref",
            _ when symbol == BuiltinIntrinsicSymbols.Core.CallCSharp => "call C#",
            _ when symbol == BuiltinIntrinsicSymbols.Core.CallCSharpCtor => "call C# ctor",
            _ when symbol == BuiltinIntrinsicSymbols.Core.LoadExternal => "load_external",
            _ when symbol == BuiltinIntrinsicSymbols.Core.StoreExternal => "store_external",
            _ => string.Empty
        };

        return capabilityName.Length > 0;
    }

    private static bool TryEncodeTypedOperation(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        out string capabilityName)
    {
        capabilityName = string.Empty;

        if (typeArguments.Count != 1)
            return false;

        if (!LegacyIntrinsicSuffixMap.TryResolveSuffix(typeArguments[0].RuntimeType, out var suffix))
            return false;

        var prefix = symbol switch
        {
            _ when symbol == BuiltinIntrinsicSymbols.Arithmetic.Add => "add",
            _ when symbol == BuiltinIntrinsicSymbols.Arithmetic.Subtract => "sub",
            _ when symbol == BuiltinIntrinsicSymbols.Arithmetic.Multiply => "mul",
            _ when symbol == BuiltinIntrinsicSymbols.Arithmetic.Divide => "div",
            _ when symbol == BuiltinIntrinsicSymbols.Comparison.Equal => "cmp_eq",
            _ when symbol == BuiltinIntrinsicSymbols.Comparison.NotEqual => "cmp_ne",
            _ when symbol == BuiltinIntrinsicSymbols.Comparison.Greater => "cmp_gt",
            _ when symbol == BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual => "cmp_ge",
            _ when symbol == BuiltinIntrinsicSymbols.Comparison.Less => "cmp_lt",
            _ when symbol == BuiltinIntrinsicSymbols.Comparison.LessOrEqual => "cmp_le",
            _ when symbol == BuiltinIntrinsicSymbols.Core.LoadConst => "load",
            _ => string.Empty
        };

        if (prefix.Length == 0)
            return false;

        capabilityName = $"{prefix}_{suffix}";
        return true;
    }
}