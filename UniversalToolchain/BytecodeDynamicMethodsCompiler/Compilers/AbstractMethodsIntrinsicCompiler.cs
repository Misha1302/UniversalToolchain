using BasicCore.Core;

namespace BytecodeDynamicMethodsCompiler.Compilers;

internal sealed class AbstractMethodsIntrinsicCompiler
{
    private readonly CilIntrinsicRegistry _registry;

    public AbstractMethodsIntrinsicCompiler()
        : this(new CilIntrinsicRegistry())
    {
    }

    internal AbstractMethodsIntrinsicCompiler(CilIntrinsicRegistry registry)
    {
        _registry = registry;
    }

    public IReadOnlyList<string> SupportedIntrinsics => _registry.SupportedIntrinsics;

    public void Compile(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var normalizedInstruction = IntrinsicInstructionNormalizer.NormalizeOrThrow(instruction);

        var name = normalizedInstruction.Operands[0].Get<string>();
        var descriptor = _registry.GetRequired(name);
        descriptor.Compile(context, normalizedInstruction, stack);
    }

    public void ProcessTypes(Instruction instruction, List<Type> stack)
    {
        IntrinsicTypeProcessor.ProcessTypes(instruction, stack);
    }

    internal static void CompileCallCSharp(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var operand = instruction.Operands[1];
        var descriptor = operand as CSharpCallDescriptor;
        var method = descriptor?.Method ?? operand.Get<MethodInfo>();
        Thrower.AssertAlways(method.DeclaringType != null);

        var parametersCount = method.GetParameters().Length;
        var stackTypes = stack.TakeLast(parametersCount).ToList();
        var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
        var useExecutionScopedProvider = descriptor?.Receiver is CSharpCallReceiver.ExecutionScopedProvider;
        if (!method.IsStatic && !useExecutionScopedProvider)
        {
            targetTypes.Insert(0, method.DeclaringType);
            stackTypes.Insert(0, method.DeclaringType);
        }

        CastValuesToTypes(context.Il, targetTypes, stackTypes);
        method = GenericTypeResolver.MakeGenericMethod(method, targetTypes);
        if (descriptor?.Receiver is CSharpCallReceiver.ExecutionScopedProvider executionScopedProvider)
        {
            Thrower.AssertAlways(
                parametersCount == 0,
                "Execution-scoped provider calls with parameters are not supported in CIL backend yet.");
            context.Il.Ldarg(0);
            context.Il.Ldtoken(executionScopedProvider.ProviderType);
            context.Il.Call(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)).NotNull());
            context.Il.Call(typeof(RuntimeCallProviderResolverExtensions)
                .GetMethod(nameof(RuntimeCallProviderResolverExtensions.GetRequiredProvider))
                .NotNull());
            context.Il.Castclass(executionScopedProvider.ProviderType);
            context.Il.Call(method);
        }
        else
        {
            context.Il.Call(method);
        }

        PopMany(stack, targetTypes.Count);
        if (method.ReturnType != typeof(void))
            stack.Push(method.ReturnType);
    }

    internal static void CompileCallCSharpCtor(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var ctor = instruction.Operands[1].Get<ConstructorInfo>();
        Thrower.AssertAlways(ctor.DeclaringType != null);

        var targetTypes = ctor.GetParameters().Select(x => x.ParameterType).ToList();
        var stackTypes = stack.TakeLast(targetTypes.Count).ToList();

        CastValuesToTypes(context.Il, targetTypes, stackTypes);
        context.Il.Newobj(ctor);

        PopMany(stack, targetTypes.Count);
        stack.Push(ctor.DeclaringType);
    }

    internal static void CompileStoreLocal(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var varName = instruction.Operands[1].Get<string>();
        var varType = instruction.Operands[2].Get<Type>();

        if (context.ExternalSlots.TryGetValue(varName, out var slot))
            context.Il.Starg(slot + context.ExternalArgumentOffset);
        else
            context.Il.Stloc(context.GetOrCreateLocal(varName, varType));

        stack.Pop();
    }

    internal static void CompileLoadLocal(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var varName = instruction.Operands[1].Get<string>();
        var varType = instruction.Operands[2].Get<Type>();

        if (context.ExternalSlots.TryGetValue(varName, out var slot))
            context.Il.Ldarg(slot + context.ExternalArgumentOffset);
        else
            context.Il.Ldloc(context.GetOrCreateLocal(varName, varType, true));

        stack.Push(varType);
    }

    internal static void CompileLoadLocalRef(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var varName = instruction.Operands[1].Get<string>();
        var varType = instruction.Operands[2].Get<Type>();

        context.Il.Ldloca(context.GetOrCreateLocal(varName, varType, true));
        stack.Push(varType.MakeByRefType());
    }

    internal static void CompileLoadExternal(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var slot = instruction.Operands[1].Get<int>();
        var varType = instruction.Operands[2].Get<Type>();
        context.Il.Ldarg(slot + context.ExternalArgumentOffset);
        stack.Push(varType);
    }

    internal static void CompileStoreExternal(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var slot = instruction.Operands[1].Get<int>();
        context.Il.Starg(slot + context.ExternalArgumentOffset);
        stack.Pop();
    }


    internal static void CompileLoadBool(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var value = instruction.Operands[1].Get<bool>();
        context.Il.Ldc_I4(value ? 1 : 0);
        stack.Push(typeof(bool));
    }

    internal static void CompileBooleanAnd(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        EnsureBinaryBoolOperands(stack, instruction.Operands[0].Get<string>());
        context.Il.And();
        PopTwoPush(stack, typeof(bool));
    }

    internal static void CompileBooleanOr(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        EnsureBinaryBoolOperands(stack, instruction.Operands[0].Get<string>());
        context.Il.Or();
        PopTwoPush(stack, typeof(bool));
    }

    internal static void CompileBooleanNot(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        Thrower.AssertAlways(stack.Count >= 1, $"Not enough values on stack for {instruction.Operands[0].Get<string>()}");
        Thrower.AssertAlways(stack[^1] == typeof(bool), "Expected boolean operand");

        context.Il.Ldc_I4(1);
        context.Il.Xor();
        stack.Pop();
        stack.Push(typeof(bool));
    }

    internal static void LoadNativeNumber(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var name = instruction.Operands[0].Get<string>();
        var arg = instruction.Operands[1];

        if (name == "load_i32")
        {
            context.Il.Ldc_I4(arg.Get<int>());
            stack.Push(typeof(int));
            return;
        }

        if (name == "load_i64")
        {
            context.Il.Ldc_I8(arg.Get<long>());
            stack.Push(typeof(long));
            return;
        }

        if (name == "load_f32")
        {
            context.Il.Ldc_R4(arg.Get<float>());
            stack.Push(typeof(float));
            return;
        }

        if (name == "load_f64")
        {
            context.Il.Ldc_R8(arg.Get<double>());
            stack.Push(typeof(double));
            return;
        }

        if (name == "load_decimal")
        {
            var dec = arg.Get<decimal>();
            EmitDecimalLiteral(context.Il, dec);
            stack.Push(typeof(decimal));
            return;
        }

        Thrower.InvalidOpEx($"Unknown native number loading {name}");
    }

    private static void EmitDecimalLiteral(GroboIL il, decimal value)
    {
        var bits = decimal.GetBits(value);
        var sign = (bits[3] & 0x80000000) != 0;
        var scale = (byte)(bits[3] >> 16 & 0x7f);

        il.Ldc_I4(bits[0]);
        il.Ldc_I4(bits[1]);
        il.Ldc_I4(bits[2]);
        il.Ldc_I4(sign ? 1 : 0);
        il.Ldc_I4(scale);

        var ctor = typeof(decimal).GetConstructor([typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte)]);
        il.Newobj(ctor);
    }

    internal static void CompileArithmeticIntrinsic(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var name = instruction.Operands[0].Get<string>();
        var (_, operation, operandType) = ParseIntrinsicSignature(name);

        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for binary operation {name}");

        if (operandType == typeof(decimal))
        {
            CompileDecimalArithmetic(context.Il, operation);
            PopTwoPush(stack, typeof(decimal));
            return;
        }

        Thrower.AssertAlways(stack[^1] == operandType && stack[^2] == operandType, $"Type mismatch for operation {name}");

        CompilePrimitiveArithmetic(context.Il, operation);
        PopTwoPush(stack, operandType);
    }

    private static void CompileDecimalArithmetic(GroboIL il, string operation)
    {
        string methodName;
        if (operation == "add")
        {
            methodName = "Add";
        }
        else if (operation == "sub")
        {
            methodName = "Subtract";
        }
        else if (operation == "mul")
        {
            methodName = "Multiply";
        }
        else if (operation == "div")
        {
            methodName = "Divide";
        }
        else
        {
            Thrower.InvalidOpEx($"Unknown decimal operation: {operation}");
            return;
        }

        var method = typeof(decimal).GetMethod(methodName, [typeof(decimal), typeof(decimal)]);
        il.Call(method);
    }

    private static void CompilePrimitiveArithmetic(GroboIL il, string operation)
    {
        if (operation == "add")
            il.Add();
        else if (operation == "sub")
            il.Sub();
        else if (operation == "mul")
            il.Mul();
        else if (operation == "div")
            il.Div(false);
        else
            Thrower.InvalidOpEx($"Unknown operation: {operation}");
    }

    internal static void CompileComparisonIntrinsic(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var name = instruction.Operands[0].Get<string>();
        var (_, operation, operandType) = ParseIntrinsicSignature(name);

        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for comparison {name}");

        Thrower.AssertAlways(stack[^1] == operandType && stack[^2] == operandType, $"Type mismatch for operation {name}");

        CompilePrimitiveComparison(context.Il, operation);
        PopTwoPush(stack, typeof(bool));
    }

    private static void CompilePrimitiveComparison(GroboIL il, string operation)
    {
        if (operation == "eq")
        {
            il.Ceq();
        }
        else if (operation == "ne")
        {
            il.Ceq();
            il.Ldc_I4(0);
            il.Ceq();
        }
        else if (operation == "gt")
        {
            il.Cgt(false);
        }
        else if (operation == "ge")
        {
            il.Clt(false);
            il.Ldc_I4(0);
            il.Ceq();
        }
        else if (operation == "lt")
        {
            il.Clt(false);
        }
        else if (operation == "le")
        {
            il.Cgt(false);
            il.Ldc_I4(0);
            il.Ceq();
        }
        else
        {
            Thrower.InvalidOpEx($"Unknown comparison operation: {operation}");
        }
    }

    private static Type GetTypeFromString(string typeStr)
    {
        if (typeStr == "i32")
            return typeof(int);
        if (typeStr == "i64")
            return typeof(long);
        if (typeStr == "f32")
            return typeof(float);
        if (typeStr == "f64")
            return typeof(double);
        if (typeStr == "decimal")
            return typeof(decimal);

        Thrower.InvalidOpEx($"Unsupported type string: {typeStr}");
        return typeof(void);
    }

    private static Type GetLoadIntrinsicType(string name)
    {
        return name switch
        {
            "load_i32" => typeof(int),
            "load_i64" => typeof(long),
            "load_f32" => typeof(float),
            "load_f64" => typeof(double),
            "load_decimal" => typeof(decimal),
            _ => Thrower.InvalidOpEx<Type>($"Unknown native number loading {name}")
        };
    }

    private static (string Family, string Operation, Type OperandType) ParseIntrinsicSignature(string name)
    {
        var parts = name.Split('_');
        Thrower.AssertAlways(parts.Length >= 2, $"Unsupported intrinsic name format: {name}");

        if (parts[0] == "cmp")
        {
            Thrower.AssertAlways(parts.Length == 3, $"Unsupported comparison intrinsic name format: {name}");
            return ("cmp", parts[1], GetTypeFromString(parts[2]));
        }

        Thrower.AssertAlways(parts.Length == 2, $"Unsupported intrinsic name format: {name}");
        return (parts[0], parts[0], GetTypeFromString(parts[1]));
    }

    private static void CastValuesToTypes(GroboIL il, IReadOnlyList<Type> targetTypes, IReadOnlyList<Type> stackTypes)
    {
        Thrower.AssertAlways(targetTypes.Count == stackTypes.Count);

        var needCasting = false;
        for (var i = 0; i < targetTypes.Count; i++)
        {
            if (!NeedsCast(targetTypes[i], stackTypes[i]))
                continue;

            needCasting = true;
            break;
        }

        if (!needCasting)
            return;

        var locals = new GroboIL.Local[targetTypes.Count];
        for (var i = targetTypes.Count - 1; i >= 0; i--)
        {
            var sourceType = stackTypes[i];
            locals[i] = il.DeclareLocal(sourceType);
            il.Stloc(locals[i]);
        }

        for (var i = 0; i < targetTypes.Count; i++)
        {
            var sourceType = stackTypes[i];
            var targetType = targetTypes[i];

            il.Ldloc(locals[i]);
            EmitCast(il, sourceType, targetType);
        }
    }

    private static bool NeedsCast(Type targetType, Type sourceType)
    {
        if (targetType == sourceType)
            return false;

        if (targetType.IsByRef || sourceType.IsByRef)
        {
            Thrower.AssertAlways(targetType == sourceType, $"Cannot cast {sourceType} to {targetType}");
            return false;
        }

        if (sourceType.IsValueType && !targetType.IsValueType)
            return true;

        if (!sourceType.IsValueType && !targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return false;

        Thrower.InvalidOpEx($"Cannot cast {sourceType} to {targetType}");
        return false;
    }

    private static void EmitCast(GroboIL il, Type sourceType, Type targetType)
    {
        if (targetType == sourceType)
            return;

        if (targetType.IsByRef || sourceType.IsByRef)
        {
            Thrower.AssertAlways(targetType == sourceType, $"Cannot cast {sourceType} to {targetType}");
            return;
        }

        if (sourceType.IsValueType && !targetType.IsValueType)
        {
            il.Box(sourceType);
            if (targetType != typeof(object))
                il.Castclass(targetType);
            return;
        }

        if (!sourceType.IsValueType && !targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return;

        Thrower.InvalidOpEx($"Cannot cast {sourceType} to {targetType}");
    }

    private static void EnsureBinaryBoolOperands(List<Type> stack, string intrinsicName)
    {
        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for {intrinsicName}");
        Thrower.AssertAlways(stack[^1] == typeof(bool) && stack[^2] == typeof(bool),
            $"Expected bool operands for {intrinsicName}");
    }

    private static void PopTwoPush(List<Type> stack, Type resultType)
    {
        stack.Pop();
        stack.Pop();
        stack.Push(resultType);
    }

    private static void PopMany(List<Type> stack, int count)
    {
        for (var i = 0; i < count; i++)
            stack.Pop();
    }
}