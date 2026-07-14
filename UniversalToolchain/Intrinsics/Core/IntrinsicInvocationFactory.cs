using BasicCore.Builtins;
using BasicCore.Capabilities;
using BasicCore.Contracts;

namespace BasicCore.Core;

public static class IntrinsicInvocationFactory
{
    public static IntrinsicInvocation ForCapability(
        string capabilityId,
        IReadOnlyList<object?>? dataOperands = null)
    {
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("Intrinsic capability identifier must not be empty.", nameof(capabilityId));

        var id = capabilityId.Trim();
        var operands = dataOperands ?? [];

        if (TryResolveBuiltin(id, operands, out var invocation))
            return invocation;

        return new IntrinsicInvocation(
            new IntrinsicSymbol(IntrinsicCapabilityNameEncoder.CapabilityNamespace, id),
            [],
            operands);
    }

    private static bool TryResolveBuiltin(
        string capabilityId,
        IReadOnlyList<object?> dataOperands,
        out IntrinsicInvocation invocation)
    {
        invocation = capabilityId switch
        {
            IntrinsicCapabilityIds.CallCSharp => Create(BuiltinIntrinsicSymbols.Core.CallCSharp, dataOperands),
            IntrinsicCapabilityIds.CallCSharpConstructor => Create(BuiltinIntrinsicSymbols.Core.CallCSharpCtor, dataOperands),
            IntrinsicCapabilityIds.BooleanAnd => Create(BuiltinIntrinsicSymbols.Boolean.And, dataOperands),
            IntrinsicCapabilityIds.BooleanOr => Create(BuiltinIntrinsicSymbols.Boolean.Or, dataOperands),
            IntrinsicCapabilityIds.BooleanNot => Create(BuiltinIntrinsicSymbols.Boolean.Not, dataOperands),
            IntrinsicCapabilityIds.LoadLocal => CreateWithOptionalRuntimeType(BuiltinIntrinsicSymbols.Storage.LoadLocal, dataOperands, 1),
            IntrinsicCapabilityIds.StoreLocal => CreateWithOptionalRuntimeType(BuiltinIntrinsicSymbols.Storage.StoreLocal, dataOperands, 1),
            IntrinsicCapabilityIds.LoadLocalReference => CreateWithOptionalRuntimeType(BuiltinIntrinsicSymbols.Storage.LoadLocalRef, dataOperands, 1),
            IntrinsicCapabilityIds.LoadExternal => CreateWithOptionalRuntimeType(BuiltinIntrinsicSymbols.Core.LoadExternal, dataOperands, 1),
            IntrinsicCapabilityIds.StoreExternal => Create(BuiltinIntrinsicSymbols.Core.StoreExternal, dataOperands),
            _ => default!
        };

        if (invocation is not null)
            return true;

        if (TryResolveTypedCapability(capabilityId, out var symbol, out var runtimeType))
        {
            invocation = new IntrinsicInvocation(
                symbol,
                [IntrinsicTypeArgument.From(runtimeType)],
                dataOperands);
            return true;
        }

        return false;
    }

    private static bool TryResolveTypedCapability(
        string capabilityId,
        out IntrinsicSymbol symbol,
        out Type runtimeType)
    {
        symbol = default;
        runtimeType = default!;

        var separator = capabilityId.LastIndexOf('_');
        if (separator <= 0 || separator == capabilityId.Length - 1 ||
            !IntrinsicTypeTokenMap.TryResolveType(capabilityId[(separator + 1)..], out runtimeType))
        {
            return false;
        }

        var operation = capabilityId[..separator];
        symbol = operation switch
        {
            "load" => BuiltinIntrinsicSymbols.Core.LoadConst,
            "add" => BuiltinIntrinsicSymbols.Arithmetic.Add,
            "sub" => BuiltinIntrinsicSymbols.Arithmetic.Subtract,
            "mul" => BuiltinIntrinsicSymbols.Arithmetic.Multiply,
            "div" => BuiltinIntrinsicSymbols.Arithmetic.Divide,
            "cmp_eq" => BuiltinIntrinsicSymbols.Comparison.Equal,
            "cmp_ne" => BuiltinIntrinsicSymbols.Comparison.NotEqual,
            "cmp_gt" => BuiltinIntrinsicSymbols.Comparison.Greater,
            "cmp_ge" => BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual,
            "cmp_lt" => BuiltinIntrinsicSymbols.Comparison.Less,
            "cmp_le" => BuiltinIntrinsicSymbols.Comparison.LessOrEqual,
            _ => default
        };

        return symbol != default;
    }

    private static IntrinsicInvocation Create(
        IntrinsicSymbol symbol,
        IReadOnlyList<object?> dataOperands) =>
        new(symbol, [], dataOperands);

    private static IntrinsicInvocation CreateWithOptionalRuntimeType(
        IntrinsicSymbol symbol,
        IReadOnlyList<object?> dataOperands,
        int typeOperandIndex)
    {
        var typeArguments = dataOperands.Count > typeOperandIndex &&
                            dataOperands[typeOperandIndex] is Type runtimeType
            ? new[] { IntrinsicTypeArgument.From(runtimeType) }
            : [];

        return new IntrinsicInvocation(symbol, typeArguments, dataOperands);
    }
}
