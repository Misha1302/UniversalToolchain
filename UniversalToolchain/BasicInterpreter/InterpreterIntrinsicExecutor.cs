using BasicCore.Capabilities;
using BasicCore.Contracts;
using BasicCore.Builtins;
using BasicCore.Core;
using BasicCore.Execution;
using IntermediateRepresentationAbstractions;

namespace BasicInterpreter;

internal sealed class InterpreterIntrinsicExecutor
{
    public void Execute(Instruction instruction, InterpreterState state)
    {
        var intrinsic = IntrinsicInstructionView.ReadOrThrow(instruction);
        var intrinsicName = intrinsic.CapabilityId;
        if (intrinsic.Invocation.Symbol == BuiltinIntrinsicSymbols.Core.CallCSharp || intrinsicName == IntrinsicCapabilityIds.CallCSharp)
        {
            ExecuteCallCSharp(intrinsic.Invocation, state);
            return;
        }
        if (intrinsic.Invocation.Symbol == BuiltinIntrinsicSymbols.Core.CallCSharpCtor || intrinsicName == IntrinsicCapabilityIds.CallCSharpConstructor)
        {
            ExecuteCallCSharpCtor(intrinsic.Invocation, state);
            return;
        }
        Thrower.InvalidOpEx($"Interpreter backend supports only 'call C#' and 'call C# ctor' intrinsics. Unsupported intrinsic: '{intrinsicName}'.");
    }

    private static void ExecuteCallCSharp(IntrinsicInvocation invocation, InterpreterState state)
    {
        var operand = invocation.GetRequiredDataOperand(0);
        var descriptor = operand as IManagedCallDescriptor;
        var method = descriptor?.Method ?? operand.Get<MethodInfo>();
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        var argumentTypes = new Type[parameters.Length];
        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.EvaluationStackCount == 0)
                Thrower.InvalidOpEx("Cannot call method: not enough arguments on the interpreter stack.");
            var entry = state.PopEvaluationValue();
            if (parameters[i].ParameterType.IsByRef)
                Thrower.InvalidOpEx("By-ref call is not supported by the interpreter intrinsic surface.");
            args[i] = entry.Value;
            argumentTypes[i] = entry.DeclaredType;
        }

        var targetTypes = GenericTypeResolver.GetParameterTypes(method, argumentTypes).ToList();
        for (var i = 0; i < args.Length; i++)
        {
            if (targetTypes[i].IsByRef)
                continue;
            if (args[i] != null && targetTypes[i].IsInstanceOfType(args[i]))
                continue;
            args[i] = RuntimeValueConversion.Convert(args[i], targetTypes[i]);
        }
        method = GenericTypeResolver.MakeGenericMethod(method, targetTypes.ToArray());

        object? result;
        if (descriptor?.ReceiverKind == ManagedCallReceiverKind.ExecutionScopedProvider)
        {
            var providerType = descriptor.ExecutionScopedProviderType.NotNull();
            var provider = state.ExecutionEnvironment.NotNull().GetRequiredProvider(providerType);
            result = method.Invoke(provider, args);
        }
        else if (method.IsStatic)
        {
            result = method.Invoke(null, args);
        }
        else
        {
            if (state.EvaluationStackCount == 0)
                Thrower.InvalidOpEx("Cannot call instance method: object instance is missing on the interpreter stack.");
            result = method.Invoke(state.PopEvaluationValue().Value, args);
        }

        if (method.ReturnType != typeof(void))
            state.PushEvaluationValue(result, method.ReturnType);
    }

    private static void ExecuteCallCSharpCtor(IntrinsicInvocation invocation, InterpreterState state)
    {
        var ctor = invocation.GetRequiredDataOperand<ConstructorInfo>(0);
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.EvaluationStackCount == 0)
                Thrower.InvalidOpEx("Cannot call constructor: not enough arguments on the interpreter stack.");
            var value = state.PopEvaluationValue().Value;
            args[i] = value != null && parameters[i].ParameterType.IsInstanceOfType(value)
                ? value
                : RuntimeValueConversion.Convert(value, parameters[i].ParameterType);
        }
        var instance = ctor.Invoke(args);
        state.PushEvaluationValue(instance.NotNull(), ctor.DeclaringType.NotNull());
    }
}
