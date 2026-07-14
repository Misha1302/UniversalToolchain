using BasicCore.Builtins;
using BasicCore.Contracts;

namespace BasicCore.Capabilities;

/// <summary>
/// Encodes structured intrinsic symbols into stable backend capability identifiers.
/// Capability identifiers are metadata only; they are never decoded as AIR instruction payloads.
/// </summary>
public static class IntrinsicCapabilityNameEncoder
{
    public const string CapabilityNamespace = "Capability";

    public static bool TryEncode(IntrinsicInvocation invocation, out string capabilityName)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        return TryEncode(invocation.Symbol, invocation.TypeArguments, out capabilityName);
    }

    public static bool TryEncode(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        out string capabilityName)
    {
        ArgumentNullException.ThrowIfNull(typeArguments);

        if (string.Equals(symbol.Namespace, CapabilityNamespace, StringComparison.Ordinal))
        {
            capabilityName = symbol.Name;
            return !string.IsNullOrWhiteSpace(capabilityName);
        }

        if (TryEncodeTypedOperation(symbol, typeArguments, out capabilityName))
            return true;

        capabilityName = symbol switch
        {
            _ when symbol == BuiltinIntrinsicSymbols.Boolean.And => IntrinsicCapabilityIds.BooleanAnd,
            _ when symbol == BuiltinIntrinsicSymbols.Boolean.Or => IntrinsicCapabilityIds.BooleanOr,
            _ when symbol == BuiltinIntrinsicSymbols.Boolean.Not => IntrinsicCapabilityIds.BooleanNot,
            _ when symbol == BuiltinIntrinsicSymbols.Storage.LoadLocal => IntrinsicCapabilityIds.LoadLocal,
            _ when symbol == BuiltinIntrinsicSymbols.Storage.StoreLocal => IntrinsicCapabilityIds.StoreLocal,
            _ when symbol == BuiltinIntrinsicSymbols.Storage.LoadLocalRef => IntrinsicCapabilityIds.LoadLocalReference,
            _ when symbol == BuiltinIntrinsicSymbols.Core.CallCSharp => IntrinsicCapabilityIds.CallCSharp,
            _ when symbol == BuiltinIntrinsicSymbols.Core.CallCSharpCtor => IntrinsicCapabilityIds.CallCSharpConstructor,
            _ when symbol == BuiltinIntrinsicSymbols.Core.LoadExternal => IntrinsicCapabilityIds.LoadExternal,
            _ when symbol == BuiltinIntrinsicSymbols.Core.StoreExternal => IntrinsicCapabilityIds.StoreExternal,
            _ => string.Empty
        };

        return capabilityName.Length > 0;
    }

    public static string EncodeOrThrow(IntrinsicInvocation invocation) =>
        TryEncode(invocation, out var capabilityName)
            ? capabilityName
            : throw new InvalidOperationException(
                $"Intrinsic '{invocation.Symbol}' cannot be mapped to a backend capability identifier.");

    private static bool TryEncodeTypedOperation(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        out string capabilityName)
    {
        capabilityName = string.Empty;

        if (typeArguments.Count != 1)
            return false;

        if (!IntrinsicTypeTokenMap.TryResolveToken(typeArguments[0].RuntimeType, out var suffix))
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
