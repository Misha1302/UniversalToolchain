using BasicCore.Builtins;
using BasicCore.Capabilities;
using BasicCore.Core;
using IntermediateRepresentationAbstractions;

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
        _registry = registry.ArgNotNull();
    }

    public IReadOnlyList<string> SupportedIntrinsics => _registry.SupportedIntrinsics;

    public void Compile(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var intrinsic = IntrinsicInstructionView.ReadOrThrow(instruction);
        var descriptor = _registry.GetRequired(intrinsic.CapabilityId);
        descriptor.Compile(context, instruction, stack);
    }

    public void ProcessTypes(Instruction instruction, List<Type> stack)
    {
        IntrinsicTypeProcessor.ProcessTypes(instruction, stack);
    }

    internal static void CompileCallCSharp(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var invocation = IntrinsicInstructionView.ReadOrThrow(instruction).Invocation;
        var operand = invocation.GetRequiredDataOperand(0);
        var descriptor = operand as IManagedCallDescriptor;
        var method = descriptor?.Method ?? operand.Get<MethodInfo>();
        Thrower.AssertAlways(method.DeclaringType != null);

        var parametersCount = method.GetParameters().Length;
        var stackTypes = stack.TakeLast(parametersCount).ToList();
        var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
        var useExecutionScopedProvider = descriptor?.ReceiverKind == ManagedCallReceiverKind.ExecutionScopedProvider;
        if (!method.IsStatic && !useExecutionScopedProvider)
        {
            targetTypes.Insert(0, method.DeclaringType);
            stackTypes.Insert(0, method.DeclaringType);
        }

        CastValuesToTypes(context.Il, targetTypes, stackTypes);
        method = GenericTypeResolver.MakeGenericMethod(method, targetTypes);
        if (useExecutionScopedProvider)
        {
            Thrower.AssertAlways(
                context.ExecutionEnvironmentArgumentIndex.HasValue,
                "Execution-scoped provider calls require an execution environment argument.");
            var providerType = descriptor!.ExecutionScopedProviderType.NotNull();
            var argumentLocals = new GroboIL.Local[targetTypes.Count];
            for (var i = targetTypes.Count - 1; i >= 0; i--)
            {
                argumentLocals[i] = context.Il.DeclareLocal(targetTypes[i]);
                context.Il.Stloc(argumentLocals[i]);
            }
            context.Il.Ldarg(context.ExecutionEnvironmentArgumentIndex.Value);
            context.Il.Ldtoken(providerType);
            context.Il.Call(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)).NotNull());
            context.Il.Call(typeof(RuntimeCallProviderResolverExtensions)
                .GetMethod(nameof(RuntimeCallProviderResolverExtensions.GetRequiredProvider))
                .NotNull());
            context.Il.Castclass(providerType);
            for (var i = 0; i < argumentLocals.Length; i++)
                context.Il.Ldloc(argumentLocals[i]);
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
        var invocation = IntrinsicInstructionView.ReadOrThrow(instruction).Invocation;
        var ctor = invocation.GetRequiredDataOperand<ConstructorInfo>(0);
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
        var invocation = IntrinsicInstructionView.ReadOrThrow(instruction).Invocation;
        var varName = invocation.GetRequiredDataOperand<string>(0);
        var varType = GetRuntimeType(invocation, IntrinsicInstructionView.ReadOrThrow(instruction).CapabilityId, dataOperandTypeIndex: 1);

        if (context.ExternalSlots.TryGetValue(varName, out var slot))
            context.Il.Starg(slot + context.ExternalArgumentOffset);
        else
            context.Il.Stloc(context.GetOrCreateLocal(varName, varType));

        stack.Pop();
    }

    internal static void CompileLoadLocal(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var invocation = IntrinsicInstructionView.ReadOrThrow(instruction).Invocation;
        var varName = invocation.GetRequiredDataOperand<string>(0);
        var varType = GetRuntimeType(invocation, IntrinsicInstructionView.ReadOrThrow(instruction).CapabilityId, dataOperandTypeIndex: 1);

        if (context.ExternalSlots.TryGetValue(varName, out var slot))
            context.Il.Ldarg(slot + context.ExternalArgumentOffset);
        else
            context.Il.Ldloc(context.GetOrCreateLocal(varName, varType, true));

        stack.Push(varType);
    }

    internal static void CompileLoadLocalRef(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var invocation = IntrinsicInstructionView.ReadOrThrow(instruction).Invocation;
        var varName = invocation.GetRequiredDataOperand<string>(0);
        var varType = GetRuntimeType(invocation, IntrinsicInstructionView.ReadOrThrow(instruction).CapabilityId, dataOperandTypeIndex: 1);

        context.Il.Ldloca(context.GetOrCreateLocal(varName, varType, true));
        stack.Push(varType.MakeByRefType());
    }

    internal static void CompileLoadExternal(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var invocation = IntrinsicInstructionView.ReadOrThrow(instruction).Invocation;
        var slot = invocation.GetRequiredDataOperand<int>(0);
        var varType = GetRuntimeType(invocation, IntrinsicInstructionView.ReadOrThrow(instruction).CapabilityId, dataOperandTypeIndex: 1);
        context.Il.Ldarg(slot + context.ExternalArgumentOffset);
        stack.Push(varType);
    }

    internal static void CompileStoreExternal(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var invocation = IntrinsicInstructionView.ReadOrThrow(instruction).Invocation;
        var slot = invocation.GetRequiredDataOperand<int>(0);
        context.Il.Starg(slot + context.ExternalArgumentOffset);
        stack.Pop();
    }


    internal static void CompileLoadBool(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var invocation = IntrinsicInstructionView.ReadOrThrow(instruction).Invocation;
        var value = invocation.GetRequiredDataOperand<bool>(0);
        context.Il.Ldc_I4(value ? 1 : 0);
        stack.Push(typeof(bool));
    }

    internal static void CompileBooleanAnd(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        EnsureBinaryBoolOperands(stack, IntrinsicInstructionView.ReadOrThrow(instruction).CapabilityId);
        context.Il.And();
        PopTwoPush(stack, typeof(bool));
    }

    internal static void CompileBooleanOr(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        EnsureBinaryBoolOperands(stack, IntrinsicInstructionView.ReadOrThrow(instruction).CapabilityId);
        context.Il.Or();
        PopTwoPush(stack, typeof(bool));
    }

    internal static void CompileBooleanNot(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var intrinsicName = IntrinsicInstructionView.ReadOrThrow(instruction).CapabilityId;
        Thrower.AssertAlways(stack.Count >= 1, $"Not enough values on stack for {intrinsicName}");
        Thrower.AssertAlways(stack[^1] == typeof(bool), "Expected boolean operand");

        context.Il.Ldc_I4(1);
        context.Il.Xor();
        stack.Pop();
        stack.Push(typeof(bool));
    }

    internal static void LoadNativeNumber(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var invocation = IntrinsicInstructionView.ReadOrThrow(instruction).Invocation;
        var intrinsic = IntrinsicInstructionView.ReadOrThrow(instruction);
        var runtimeType = GetRuntimeType(invocation, intrinsic.CapabilityId);
        var value = invocation.GetRequiredDataOperand(0);

        if (runtimeType == typeof(int))
            context.Il.Ldc_I4(value.Get<int>());
        else if (runtimeType == typeof(long))
            context.Il.Ldc_I8(value.Get<long>());
        else if (runtimeType == typeof(float))
            context.Il.Ldc_R4(value.Get<float>());
        else if (runtimeType == typeof(double))
            context.Il.Ldc_R8(value.Get<double>());
        else if (runtimeType == typeof(decimal))
            EmitDecimalLiteral(context.Il, value.Get<decimal>());
        else
            Thrower.InvalidOpEx($"Unsupported native constant type '{runtimeType}'.");

        stack.Push(runtimeType);
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
        var intrinsic = IntrinsicInstructionView.ReadOrThrow(instruction);
        var operandType = GetRuntimeType(intrinsic.Invocation, intrinsic.CapabilityId);
        var operation = GetArithmeticOperation(intrinsic.Invocation.Symbol, intrinsic.CapabilityId);

        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for binary operation {intrinsic.CapabilityId}");

        if (operandType == typeof(decimal))
        {
            CompileDecimalArithmetic(context.Il, operation);
            PopTwoPush(stack, typeof(decimal));
            return;
        }

        Thrower.AssertAlways(
            stack[^1] == operandType && stack[^2] == operandType,
            $"Type mismatch for operation {intrinsic.CapabilityId}");

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
        var intrinsic = IntrinsicInstructionView.ReadOrThrow(instruction);
        var operandType = GetRuntimeType(intrinsic.Invocation, intrinsic.CapabilityId);
        var operation = GetComparisonOperation(intrinsic.Invocation.Symbol, intrinsic.CapabilityId);

        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for comparison {intrinsic.CapabilityId}");
        Thrower.AssertAlways(
            stack[^1] == operandType && stack[^2] == operandType,
            $"Type mismatch for operation {intrinsic.CapabilityId}");

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

    private static string GetArithmeticOperation(IntrinsicSymbol symbol, string capabilityId)
    {
        if (symbol == BuiltinIntrinsicSymbols.Arithmetic.Add) return "add";
        if (symbol == BuiltinIntrinsicSymbols.Arithmetic.Subtract) return "sub";
        if (symbol == BuiltinIntrinsicSymbols.Arithmetic.Multiply) return "mul";
        if (symbol == BuiltinIntrinsicSymbols.Arithmetic.Divide) return "div";

        var separator = capabilityId.IndexOf('_');
        return separator > 0
            ? capabilityId[..separator]
            : Thrower.InvalidOpEx<string>($"Unsupported arithmetic intrinsic symbol '{symbol}'.");
    }

    private static string GetComparisonOperation(IntrinsicSymbol symbol, string capabilityId)
    {
        if (symbol == BuiltinIntrinsicSymbols.Comparison.Equal) return "eq";
        if (symbol == BuiltinIntrinsicSymbols.Comparison.NotEqual) return "ne";
        if (symbol == BuiltinIntrinsicSymbols.Comparison.Greater) return "gt";
        if (symbol == BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual) return "ge";
        if (symbol == BuiltinIntrinsicSymbols.Comparison.Less) return "lt";
        if (symbol == BuiltinIntrinsicSymbols.Comparison.LessOrEqual) return "le";

        var parts = capabilityId.Split('_');
        return parts.Length == 3 && parts[0] == "cmp"
            ? parts[1]
            : Thrower.InvalidOpEx<string>($"Unsupported comparison intrinsic symbol '{symbol}'.");
    }

    private static Type GetRuntimeType(
        IntrinsicInvocation invocation,
        string capabilityId,
        int? dataOperandTypeIndex = null)
    {
        if (invocation.TypeArguments.Count == 1)
            return invocation.TypeArguments[0].RuntimeType;

        if (dataOperandTypeIndex.HasValue &&
            invocation.DataOperands.Count > dataOperandTypeIndex.Value &&
            invocation.DataOperands[dataOperandTypeIndex.Value] is Type dataType)
        {
            return dataType;
        }

        var token = capabilityId[(capabilityId.LastIndexOf('_') + 1)..];
        return IntrinsicTypeTokenMap.TryResolveType(token, out var runtimeType)
            ? runtimeType
            : Thrower.InvalidOpEx<Type>(
                $"Intrinsic '{capabilityId}' requires one type argument or a known type token.");
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
