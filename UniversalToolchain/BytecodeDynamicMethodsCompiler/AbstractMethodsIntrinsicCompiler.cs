using System.Reflection;
using DotnetHelper;
using ExceptionsManager;
using GrEmit;
using IntermediateRepresentationAbstractions;
using ListExtensions;
using ObjectExtensions;

namespace BytecodeDynamicMethodsCompiler;

internal sealed class AbstractMethodsIntrinsicCompiler
{
    private readonly Dictionary<string, IntrinsicHandler> _intrinsicHandlers;

    public AbstractMethodsIntrinsicCompiler()
    {
        _intrinsicHandlers = new Dictionary<string, IntrinsicHandler>
        {
            ["call C#"] = CompileCallCSharp,
            ["call C# ctor"] = CompileCallCSharpCtor,
            ["store_local"] = CompileStoreLocal,
            ["load_local"] = CompileLoadLocal,
            ["load_local_ref"] = CompileLoadLocalRef,
            ["load_bool"] = CompileLoadBool,
            ["boolean_and"] = CompileBooleanAnd,
            ["boolean_or"] = CompileBooleanOr,
            ["boolean_not"] = CompileBooleanNot,
            ["load_i32"] = LoadNativeNumber,
            ["load_i64"] = LoadNativeNumber,
            ["load_f32"] = LoadNativeNumber,
            ["load_f64"] = LoadNativeNumber,
            ["load_decimal"] = LoadNativeNumber
        };
    }

    public IReadOnlyList<string> SupportedIntrinsics =>
    [
        "call C#", "call C# ctor",
        "store_local", "load_local", "load_local_ref",
        "load_i32", "load_i64", "load_f32", "load_f64", "load_decimal",
        "boolean_and", "boolean_or", "boolean_not",
        "add_i32", "sub_i32", "mul_i32", "div_i32",
        "add_i64", "sub_i64", "mul_i64", "div_i64",
        "add_f32", "sub_f32", "mul_f32", "div_f32",
        "add_f64", "sub_f64", "mul_f64", "div_f64",
        "add_decimal", "sub_decimal", "mul_decimal", "div_decimal"
    ];

    public void Compile(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        Thrower.AssertAlways(instruction.UOpCode == UOpCode.Intrinsic);
        Thrower.AssertAlways(instruction.Operands[0] is string);

        var name = instruction.Operands[0].Get<string>();
        if (_intrinsicHandlers.TryGetValue(name, out var handler))
        {
            handler(context, instruction, stack);
            return;
        }

        if (IsArithmeticIntrinsic(name))
        {
            CompileArithmeticIntrinsic(context, name, stack);
            return;
        }

        Thrower.InvalidOpEx($"Unsupported intrinsic: {name}");
    }

    private static void CompileCallCSharp(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var method = instruction.Operands[1].Get<MethodInfo>();
        Thrower.AssertAlways(method.DeclaringType != null);

        var parametersCount = method.GetParameters().Length;
        var stackTypes = stack.TakeLast(parametersCount).ToList();
        var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
        if (!method.IsStatic)
        {
            targetTypes.Insert(0, method.DeclaringType);
            stackTypes.Insert(0, method.DeclaringType);
        }

        CastValuesToTypes(context.Il, targetTypes, stackTypes);
        method = GenericTypeResolver.MakeGenericMethod(method, targetTypes);
        context.Il.Call(method);

        PopMany(stack, targetTypes.Count);
        if (method.ReturnType != typeof(void))
            stack.Push(method.ReturnType);
    }

    private static void CompileCallCSharpCtor(CompilationContext context, Instruction instruction, List<Type> stack)
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

    private static void CompileStoreLocal(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var varName = instruction.Operands[1].Get<string>();
        var varType = instruction.Operands[2].Get<Type>();

        if (context.ParametersIndices.TryGetValue(varName, out var argIndex))
            context.Il.Starg(argIndex);
        else
            context.Il.Stloc(context.GetOrCreateLocal(varName, varType));

        stack.Pop();
    }

    private static void CompileLoadLocal(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var varName = instruction.Operands[1].Get<string>();
        var varType = instruction.Operands[2].Get<Type>();

        if (context.ParametersIndices.TryGetValue(varName, out var argIndex))
            context.Il.Ldarg(argIndex);
        else
            context.Il.Ldloc(context.GetOrCreateLocal(varName, varType, true));

        stack.Push(varType);
    }

    private static void CompileLoadLocalRef(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var varName = instruction.Operands[1].Get<string>();
        var varType = instruction.Operands[2].Get<Type>();

        context.Il.Ldloca(context.GetOrCreateLocal(varName, varType, true));
        stack.Push(varType.MakeByRefType());
    }

    private static void CompileLoadBool(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        var value = instruction.Operands[1].Get<bool>();
        context.Il.Ldc_I4(value ? 1 : 0);
        stack.Push(typeof(bool));
    }

    private static void CompileBooleanAnd(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        EnsureBinaryBoolOperands(stack, instruction.Operands[0].Get<string>());
        context.Il.And();
        PopTwoPush(stack, typeof(bool));
    }

    private static void CompileBooleanOr(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        EnsureBinaryBoolOperands(stack, instruction.Operands[0].Get<string>());
        context.Il.Or();
        PopTwoPush(stack, typeof(bool));
    }

    private static void CompileBooleanNot(CompilationContext context, Instruction instruction, List<Type> stack)
    {
        Thrower.AssertAlways(stack.Count >= 1, $"Not enough values on stack for {instruction.Operands[0].Get<string>()}");
        Thrower.AssertAlways(stack[^1] == typeof(bool), "Expected boolean operand");

        context.Il.Ldc_I4(1);
        context.Il.Xor();
        stack.Pop();
        stack.Push(typeof(bool));
    }

    private static void LoadNativeNumber(CompilationContext context, Instruction instruction, List<Type> stack)
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

    private static void CompileArithmeticIntrinsic(CompilationContext context, string name, List<Type> stack)
    {
        var parts = name.Split('_');
        var operation = parts[0];
        var typeStr = parts[1];

        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for binary operation {name}");

        if (typeStr == "decimal")
        {
            CompileDecimalArithmetic(context.Il, operation);
            PopTwoPush(stack, typeof(decimal));
            return;
        }

        var resultType = GetTypeFromString(typeStr);
        Thrower.AssertAlways(stack[^1] == resultType && stack[^2] == resultType, $"Type mismatch for operation {name}");

        CompilePrimitiveArithmetic(context.Il, operation);
        PopTwoPush(stack, resultType);
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
            return;
        }

        if (!sourceType.IsValueType && !targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return;

        Thrower.InvalidOpEx($"Cannot cast {sourceType} to {targetType}");
    }

    private static bool IsArithmeticIntrinsic(string name)
        => name.StartsWith("add_") || name.StartsWith("sub_") || name.StartsWith("mul_") || name.StartsWith("div_");

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

    private delegate void IntrinsicHandler(CompilationContext context, Instruction instruction, List<Type> stack);
}