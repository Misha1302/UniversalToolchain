using System.Reflection;
using IntermediateRepresentationAbstractions;
using BasicCore.Capabilities;

namespace BasicCore.Core;

public static class IntrinsicTypeProcessor
{
    public static void ProcessTypes(Instruction instruction, List<Type> stack)
    {
        instruction = instruction.ArgNotNull();
        stack = stack.ArgNotNull();

        var intrinsic = IntrinsicInstructionView.ReadOrThrow(instruction);
        var invocation = intrinsic.Invocation;
        var symbol = invocation.Symbol;

        if (symbol == BuiltinIntrinsicSymbols.Core.CallCSharp)
            ProcessTypesCallCSharp(invocation, intrinsic.CapabilityId, stack);
        else if (symbol == BuiltinIntrinsicSymbols.Core.CallCSharpCtor)
            ProcessTypesCallCSharpCtor(invocation, intrinsic.CapabilityId, stack);
        else if (symbol == BuiltinIntrinsicSymbols.Storage.StoreLocal)
            PopOne(intrinsic.CapabilityId, stack);
        else if (symbol == BuiltinIntrinsicSymbols.Storage.LoadLocal)
            stack.Add(invocation.GetRequiredSingleRuntimeType());
        else if (symbol == BuiltinIntrinsicSymbols.Storage.LoadLocalRef)
            stack.Add(invocation.GetRequiredSingleRuntimeType().MakeByRefType());
        else if (symbol == BuiltinIntrinsicSymbols.Core.LoadExternal)
            stack.Add(invocation.GetRequiredSingleRuntimeType());
        else if (symbol == BuiltinIntrinsicSymbols.Core.StoreExternal)
            PopOne(intrinsic.CapabilityId, stack);
        else if (symbol == BuiltinIntrinsicSymbols.Core.LoadConst)
            stack.Add(invocation.GetRequiredSingleRuntimeType());
        else if (symbol == BuiltinIntrinsicSymbols.Boolean.And || symbol == BuiltinIntrinsicSymbols.Boolean.Or)
            ProcessBooleanBinary(intrinsic.CapabilityId, stack);
        else if (symbol == BuiltinIntrinsicSymbols.Boolean.Not)
            ProcessBooleanUnary(intrinsic.CapabilityId, stack);
        else if (symbol.Namespace == BuiltinIntrinsicSymbols.Arithmetic.Add.Namespace)
            ProcessArithmetic(invocation.GetRequiredSingleRuntimeType(), intrinsic.CapabilityId, stack);
        else if (symbol.Namespace == BuiltinIntrinsicSymbols.Comparison.Equal.Namespace)
            ProcessComparison(invocation.GetRequiredSingleRuntimeType(), intrinsic.CapabilityId, stack);
        else if (string.Equals(symbol.Namespace, IntrinsicCapabilityNameEncoder.CapabilityNamespace, StringComparison.Ordinal))
            ProcessCapabilityOnlyIntrinsic(intrinsic, stack);
        else
            Thrower.InvalidOpEx($"Unknown intrinsic '{symbol}'.");
    }

    private static void ProcessTypesCallCSharp(
        IntrinsicInvocation invocation,
        string capabilityId,
        List<Type> stack)
    {
        var operand = invocation.GetRequiredDataOperand(0);
        var resolver = new MethodCallTypeSemanticsResolver();

        MethodCallResolution resolution;
        if (operand is MethodInfo method)
            resolution = resolver.ResolveForStack(method, stack);
        else if (operand is IManagedCallDescriptor descriptor)
            resolution = resolver.ResolveForStack(descriptor, stack);
        else
            resolution = Thrower.InvalidOpEx<MethodCallResolution>(
                "CallCSharp requires MethodInfo or IManagedCallDescriptor data operand.");

        Thrower.AssertAlways(
            stack.Count >= resolution.ConsumedTypes.Count,
            $"Not enough values on stack for intrinsic '{capabilityId}'.");

        PopMany(stack, resolution.ConsumedTypes.Count);
        if (resolution.ReturnType != typeof(void))
            stack.Add(resolution.ReturnType);
    }

    private static void ProcessTypesCallCSharpCtor(
        IntrinsicInvocation invocation,
        string capabilityId,
        List<Type> stack)
    {
        var ctor = invocation.GetRequiredDataOperand<ConstructorInfo>(0);
        Thrower.AssertAlways(ctor.DeclaringType != null, $"Constructor '{ctor}' must have a declaring type.");

        var parametersCount = ctor.GetParameters().Length;
        Thrower.AssertAlways(
            stack.Count >= parametersCount,
            $"Not enough values on stack for intrinsic '{capabilityId}'.");

        PopMany(stack, parametersCount);
        stack.Add(ctor.DeclaringType);
    }

    private static void ProcessCapabilityOnlyIntrinsic(
        IntrinsicInstructionView intrinsic,
        List<Type> stack)
    {
        var id = intrinsic.CapabilityId;

        if (id == IntrinsicCapabilityIds.CallCSharp)
        {
            ProcessTypesCallCSharp(intrinsic.Invocation, id, stack);
            return;
        }

        if (id == IntrinsicCapabilityIds.CallCSharpConstructor)
        {
            ProcessTypesCallCSharpCtor(intrinsic.Invocation, id, stack);
            return;
        }

        if (id == IntrinsicCapabilityIds.StoreLocal || id == IntrinsicCapabilityIds.StoreExternal)
        {
            PopOne(id, stack);
            return;
        }

        if (id is IntrinsicCapabilityIds.LoadLocal or IntrinsicCapabilityIds.LoadLocalReference or IntrinsicCapabilityIds.LoadExternal)
        {
            var runtimeType = intrinsic.DataOperands.Count >= 2
                ? intrinsic.DataOperands[1] as Type
                : null;
            if (runtimeType is null)
                throw new InvalidOperationException(
                    $"Capability intrinsic '{id}' requires a CLR Type data operand at index 1.");

            stack.Add(id == IntrinsicCapabilityIds.LoadLocalReference ? runtimeType.MakeByRefType() : runtimeType);
            return;
        }

        if (id is IntrinsicCapabilityIds.BooleanAnd or IntrinsicCapabilityIds.BooleanOr)
        {
            ProcessBooleanBinary(id, stack);
            return;
        }

        if (id == IntrinsicCapabilityIds.BooleanNot)
        {
            ProcessBooleanUnary(id, stack);
            return;
        }

        if (id.StartsWith("load_", StringComparison.Ordinal))
        {
            stack.Add(GetTypeFromCapabilityId(id, "load_"));
            return;
        }

        if (id.StartsWith("add_", StringComparison.Ordinal) ||
            id.StartsWith("sub_", StringComparison.Ordinal) ||
            id.StartsWith("mul_", StringComparison.Ordinal) ||
            id.StartsWith("div_", StringComparison.Ordinal))
        {
            ProcessArithmetic(GetTypeFromCapabilityId(id, id[..4]), id, stack);
            return;
        }

        if (id.StartsWith("cmp_", StringComparison.Ordinal))
        {
            var token = id[(id.LastIndexOf('_') + 1)..];
            ProcessComparison(GetTypeFromToken(token, id), id, stack);
            return;
        }

        Thrower.InvalidOpEx($"Unknown intrinsic capability '{id}'.");
    }

    private static Type GetTypeFromCapabilityId(string id, string prefix) =>
        GetTypeFromToken(id[prefix.Length..], id);

    private static Type GetTypeFromToken(string token, string capabilityId) =>
        IntrinsicTypeTokenMap.TryResolveType(token, out var type)
            ? type
            : Thrower.InvalidOpEx<Type>(
                $"Intrinsic capability '{capabilityId}' contains unsupported type token '{token}'.");

    private static void ProcessArithmetic(Type operandType, string capabilityId, List<Type> stack)
    {
        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for intrinsic '{capabilityId}'.");
        Thrower.AssertAlways(
            stack[^1] == operandType && stack[^2] == operandType,
            $"Type mismatch for intrinsic '{capabilityId}'.");
        PopTwoPush(stack, operandType);
    }

    private static void ProcessComparison(Type operandType, string capabilityId, List<Type> stack)
    {
        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for intrinsic '{capabilityId}'.");
        Thrower.AssertAlways(
            stack[^1] == operandType && stack[^2] == operandType,
            $"Type mismatch for intrinsic '{capabilityId}'.");
        PopTwoPush(stack, typeof(bool));
    }

    private static void ProcessBooleanBinary(string capabilityId, List<Type> stack)
    {
        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for intrinsic '{capabilityId}'.");
        Thrower.AssertAlways(
            stack[^1] == typeof(bool) && stack[^2] == typeof(bool),
            $"Intrinsic '{capabilityId}' requires two boolean operands.");
        PopTwoPush(stack, typeof(bool));
    }

    private static void ProcessBooleanUnary(string capabilityId, List<Type> stack)
    {
        Thrower.AssertAlways(stack.Count >= 1, $"Not enough values on stack for intrinsic '{capabilityId}'.");
        Thrower.AssertAlways(stack[^1] == typeof(bool), $"Intrinsic '{capabilityId}' requires a boolean operand.");
    }

    private static void PopOne(string capabilityId, List<Type> stack)
    {
        Thrower.AssertAlways(stack.Count >= 1, $"Not enough values on stack for intrinsic '{capabilityId}'.");
        stack.RemoveAt(stack.Count - 1);
    }

    private static void PopTwoPush(List<Type> stack, Type resultType)
    {
        stack.RemoveAt(stack.Count - 1);
        stack.RemoveAt(stack.Count - 1);
        stack.Add(resultType);
    }

    private static void PopMany(List<Type> stack, int count)
    {
        Thrower.AssertAlways(stack.Count >= count, $"Cannot pop {count} values from a stack of size {stack.Count}.");
        for (var i = 0; i < count; i++)
            stack.RemoveAt(stack.Count - 1);
    }
}
