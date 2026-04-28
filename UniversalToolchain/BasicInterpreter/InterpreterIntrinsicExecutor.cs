using BasicCore.Core;

namespace BasicInterpreter;

internal sealed class InterpreterIntrinsicExecutor
{
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

        if (intrinsicName == "load_bool")
        {
            ExecuteLoadBool(normalizedInstruction, state);
            return;
        }

        if (intrinsicName == "boolean_and")
        {
            ExecuteBooleanAnd(state);
            return;
        }

        if (intrinsicName == "boolean_or")
        {
            ExecuteBooleanOr(state);
            return;
        }

        if (intrinsicName == "boolean_not")
        {
            ExecuteBooleanNot(state);
            return;
        }

        if (intrinsicName == "load_external")
        {
            ExecuteLoadExternal(normalizedInstruction, state);
            return;
        }

        if (intrinsicName == "store_external")
        {
            ExecuteStoreExternal(normalizedInstruction, state);
            return;
        }

        if (intrinsicName == "load_local")
        {
            ExecuteLoadLocal(normalizedInstruction, state);
            return;
        }

        if (intrinsicName == "store_local")
        {
            ExecuteStoreLocal(normalizedInstruction, state);
            return;
        }

        if (intrinsicName == "load_local_ref")
        {
            ExecuteLoadLocalRef(normalizedInstruction, state);
            return;
        }

        if (intrinsicName.StartsWith("load_", StringComparison.Ordinal))
        {
            ExecuteLoadNativeNumber(normalizedInstruction, state);
            return;
        }

        if (intrinsicName.StartsWith("add_", StringComparison.Ordinal)
            || intrinsicName.StartsWith("sub_", StringComparison.Ordinal)
            || intrinsicName.StartsWith("mul_", StringComparison.Ordinal)
            || intrinsicName.StartsWith("div_", StringComparison.Ordinal))
        {
            ExecuteArithmeticIntrinsic(normalizedInstruction, state);
            return;
        }

        if (intrinsicName.StartsWith("cmp_", StringComparison.Ordinal))
        {
            ExecuteComparisonIntrinsic(normalizedInstruction, state);
            return;
        }

        Thrower.InvalidOpEx($"Unknown intrinsic call: {intrinsicName}.");
    }

    private void ExecuteCallCSharp(Instruction instruction, InterpreterState state)
    {
        var operand = instruction.Operands[1];
        var descriptor = operand as CSharpCallDescriptor;
        var method = descriptor?.Method ?? operand.Get<MethodInfo>();
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        var argTypes = new Type[parameters.Length];
        var byRefTargets = new Dictionary<int, LocalReference>();

        for (var i = parameters.Length - 1; i >= 0; i--)
        {
            if (state.ValueStack.Count == 0)
                Thrower.InvalidOpEx("Cannot call method: not enough arguments on the interpreter stack.");

            var value = state.ValueStack.Pop();
            if (parameters[i].ParameterType.IsByRef)
            {
                if (value is not LocalReference localReference)
                {
                    Thrower.InvalidOpEx("By-ref call requires local reference operand.");
                    return;
                }

                var byRefType = parameters[i].ParameterType.GetElementType().NotNull();
                args[i] = state.GetLocalValue(localReference.Name, byRefType);
                argTypes[i] = byRefType;
                byRefTargets[i] = localReference;
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

        foreach (var (index, localReference) in byRefTargets)
            state.SetLocalValue(localReference.Name, args[index]);

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

    private static void ExecuteLoadBool(Instruction instruction, InterpreterState state)
    {
        state.ValueStack.Push(instruction.Operands[1].Get<bool>());
    }

    private static void ExecuteLoadNativeNumber(Instruction instruction, InterpreterState state)
    {
        state.ValueStack.Push(instruction.Operands[1]);
    }

    private static void ExecuteBooleanAnd(InterpreterState state)
    {
        var right = Pop<bool>(state);
        var left = Pop<bool>(state);
        state.ValueStack.Push(left && right);
    }

    private static void ExecuteBooleanOr(InterpreterState state)
    {
        var right = Pop<bool>(state);
        var left = Pop<bool>(state);
        state.ValueStack.Push(left || right);
    }

    private static void ExecuteBooleanNot(InterpreterState state)
    {
        state.ValueStack.Push(!Pop<bool>(state));
    }

    private static void ExecuteArithmeticIntrinsic(Instruction instruction, InterpreterState state)
    {
        var name = instruction.Operands[0].Get<string>();
        var operation = name.Split('_')[0];

        if (name.EndsWith("_decimal", StringComparison.Ordinal))
        {
            var rightDecimal = Pop<decimal>(state);
            var leftDecimal = Pop<decimal>(state);
            var decimalResult = operation switch
            {
                "add" => decimal.Add(leftDecimal, rightDecimal),
                "sub" => decimal.Subtract(leftDecimal, rightDecimal),
                "mul" => decimal.Multiply(leftDecimal, rightDecimal),
                "div" => decimal.Divide(leftDecimal, rightDecimal),
                _ => Thrower.InvalidOpEx<decimal>($"Unknown arithmetic intrinsic '{name}'.")
            };
            state.ValueStack.Push(decimalResult);
            return;
        }

        var numericType = GetNumericType(name);
        var rightValue = Convert.ChangeType(state.ValueStack.Pop(), numericType);
        var leftValue = Convert.ChangeType(state.ValueStack.Pop(), numericType);

        dynamic right = rightValue.NotNull();
        dynamic left = leftValue.NotNull();

        object result = operation switch
        {
            "add" => left + right,
            "sub" => left - right,
            "mul" => left * right,
            "div" => left / right,
            _ => Thrower.InvalidOpEx<object>($"Unknown arithmetic intrinsic '{name}'.")
        };

        state.ValueStack.Push(result);
    }

    private static void ExecuteComparisonIntrinsic(Instruction instruction, InterpreterState state)
    {
        var name = instruction.Operands[0].Get<string>();
        var operation = name.Split('_')[1];

        var operandType = GetNumericType(name);
        dynamic right = Convert.ChangeType(state.ValueStack.Pop(), operandType).NotNull();
        dynamic left = Convert.ChangeType(state.ValueStack.Pop(), operandType).NotNull();

        var result = operation switch
        {
            "eq" => left == right,
            "ne" => left != right,
            "gt" => left > right,
            "ge" => left >= right,
            "lt" => left < right,
            "le" => left <= right,
            _ => Thrower.InvalidOpEx<bool>($"Unknown comparison intrinsic '{name}'.")
        };

        state.ValueStack.Push(result);
    }

    private static Type GetNumericType(string intrinsicName)
    {
        if (intrinsicName.EndsWith("_i32", StringComparison.Ordinal))
            return typeof(int);
        if (intrinsicName.EndsWith("_i64", StringComparison.Ordinal))
            return typeof(long);
        if (intrinsicName.EndsWith("_f32", StringComparison.Ordinal))
            return typeof(float);
        if (intrinsicName.EndsWith("_f64", StringComparison.Ordinal))
            return typeof(double);
        if (intrinsicName.EndsWith("_decimal", StringComparison.Ordinal))
            return typeof(decimal);

        return Thrower.InvalidOpEx<Type>($"Unknown intrinsic numeric type in '{intrinsicName}'.");
    }

    private static void ExecuteLoadExternal(Instruction instruction, InterpreterState state)
    {
        var slot = instruction.Operands[1].Get<int>();
        var value = state.ExecutionEnvironment.NotNull().GetExternalValue(slot);
        state.ValueStack.Push(value.NotNull());
    }

    private static void ExecuteStoreExternal(Instruction instruction, InterpreterState state)
    {
        var slot = instruction.Operands[1].Get<int>();
        var value = state.ValueStack.Pop();
        state.ExecutionEnvironment.NotNull().SetExternalValue(slot, value);
    }

    private static void ExecuteLoadLocal(Instruction instruction, InterpreterState state)
    {
        var name = instruction.Operands[1].Get<string>();
        var type = instruction.Operands[2].Get<Type>();
        state.ValueStack.Push(state.GetLocalValue(name, type));
    }

    private static void ExecuteStoreLocal(Instruction instruction, InterpreterState state)
    {
        var name = instruction.Operands[1].Get<string>();
        var value = state.ValueStack.Pop();
        state.SetLocalValue(name, value);
    }

    private static void ExecuteLoadLocalRef(Instruction instruction, InterpreterState state)
    {
        var name = instruction.Operands[1].Get<string>();
        var type = instruction.Operands[2].Get<Type>();
        state.GetLocalValue(name, type);
        state.ValueStack.Push(new LocalReference(name, type));
    }

    private static T Pop<T>(InterpreterState state)
    {
        if (state.ValueStack.Count == 0)
            Thrower.InvalidOpEx("Interpreter stack is empty.");

        return state.ValueStack.Pop().Get<T>();
    }

    private sealed class LocalReference(string name, Type type)
    {
        public string Name { get; } = name;
        public Type Type { get; } = type;
    }
}
