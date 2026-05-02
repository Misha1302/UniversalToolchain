using BasicCore.Core;

namespace BasicInterpreter;

internal sealed class InterpreterIntrinsicExecutor
{
    // The interpreter is the reference universal-call backend.
    // Keep this executor intentionally minimal.
    // Only "call C#" and "call C# ctor" are allowed here.
    // Feature-specific or optimization-specific intrinsics must be handled by
    // backend-capability-gated optimizers and must not be added to the interpreter.
    public void Execute(Instruction instruction, InterpreterState state)
    {
        var normalizedInstruction = IntrinsicInstructionNormalizer.NormalizeOrThrow(instruction);
        var intrinsicName = normalizedInstruction.Operands[0].Get<string>();

        if (intrinsicName == "call C#")
        {
            ExecuteCallCSharp(normalizedInstruction, state);
            return;
        }

        if (intrinsicName == "call C# ctor")
        {
            ExecuteCallCSharpCtor(normalizedInstruction, state);
            return;
        }

        Thrower.InvalidOpEx(
            $"Interpreter backend supports only 'call C#' and 'call C# ctor' intrinsics. Unsupported intrinsic: '{intrinsicName}'.");
    }

    private void ExecuteCallCSharp(Instruction instruction, InterpreterState state)
    {
        var operand = instruction.Operands[1];
        var descriptor = operand as CSharpCallDescriptor;
        var method = descriptor?.Method ?? operand.Get<MethodInfo>();
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        var argTypes = new Type[parameters.Length];
        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                Thrower.InvalidOpEx("Cannot call method: not enough arguments on the interpreter stack.");

            var value = state.ValueStack.Pop();
            if (parameters[i].ParameterType.IsByRef)
            {
                Thrower.InvalidOpEx("By-ref call is not supported by the interpreter intrinsic surface.");
                continue;
            }

            args[i] = value;
            argTypes[i] = value?.GetType() ?? typeof(object);
        }

        var stackTypes = argTypes.AsReadOnly().ToList();
        var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == null || targetTypes[i] == args[i]!.GetType() || targetTypes[i].IsByRef)
                continue;

            args[i] = ConvertValue(args[i]!, targetTypes[i]);
        }

        method = GenericTypeResolver.MakeGenericMethod(method, targetTypes.ToArray());

        object? result;
        if (descriptor?.Receiver is CSharpCallReceiver.ExecutionScopedProvider executionScopedProvider)
        {
            var environment = state.ExecutionEnvironment.NotNull();
            var provider = environment.GetRequiredProvider(executionScopedProvider.ProviderType);
            result = method.Invoke(provider, args);
        }
        else
        {
            if (method.IsStatic)
            {
                result = method.Invoke(null, args);
            }
            else
            {
                if (state.ValueStack.Count == 0)
                    Thrower.InvalidOpEx("Cannot call instance method: object instance is missing on the interpreter stack.");

                var instance = state.ValueStack.Pop();
                result = method.Invoke(instance, args);
            }
        }

        if (method.ReturnType != typeof(void))
            state.ValueStack.Push(result.NotNull());
    }

    private static object ConvertValue(object value, Type targetType)
    {
        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return value;
        }
    }

    private void ExecuteCallCSharpCtor(Instruction instruction, InterpreterState state)
    {
        var ctor = instruction.Operands[1].Get<ConstructorInfo>();
        var parameters = ctor.GetParameters();
        var args = new object[parameters.Length];

        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                Thrower.InvalidOpEx("Cannot call constructor: not enough arguments on the interpreter stack.");

            args[i] = state.ValueStack.Pop();
        }

        var instance = ctor.Invoke(args);
        state.ValueStack.Push(instance.NotNull());
    }
}