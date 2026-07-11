using BasicCore.Builtins;
using BasicCore.Capabilities;
using UniversalToolchain.Dialects.Integration;

namespace NativeMathModule;

[DialectOptimizerAlias("ArithmeticOptimization")]
[DialectRuntimeExport("Optimizer", "ArithmeticOptimization")]
[AutoRegisterService]
[IntrinsicDescriptorProvider(typeof(ArithmeticIntrinsicDescriptorProvider))]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
[UsedImplicitly]
public class ArithmeticOptimizerModule : IIRProcessingModule
{
    // Lightweight e-graph-inspired symbolic simplifier for straight-line postfix IR.
    // This is intentionally not a full equality-saturation e-graph engine.
    private static readonly IReadOnlyList<Type> _supportedArithmeticTypes =
    [
        typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)
    ];

    private IOptimizerIntrinsicCapabilityContext? _capabilityContext;

    public void InitIntrinsicCapabilityContext(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        capabilityContext = capabilityContext.ArgNotNull();

        _capabilityContext = capabilityContext;
    }

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        if (_capabilityContext == null)
            Thrower.InvalidOpEx("Arithmetic optimizer requires intrinsic capability context initialization.");

        var capabilityContext = _capabilityContext;

        if (!HasRequiredCapabilities(capabilityContext, _supportedArithmeticTypes))
            return current;

        current = OptimizeArithmetic(current);
        return current;
    }

    private static bool HasRequiredCapabilities(IOptimizerIntrinsicCapabilityContext capabilityContext, IReadOnlyList<Type> types)
    {
        var requirements = types.SelectMany(type => new (IntrinsicSymbol Symbol, Type[] TypeArguments)[]
        {
            (BuiltinIntrinsicSymbols.Arithmetic.Add, [type]),
            (BuiltinIntrinsicSymbols.Arithmetic.Subtract, [type]),
            (BuiltinIntrinsicSymbols.Arithmetic.Multiply, [type]),
            (BuiltinIntrinsicSymbols.Arithmetic.Divide, [type])
        });

        return OptimizerCapabilityGuards.SupportsAll(capabilityContext, requirements);
    }

    private IAbstractIR OptimizeArithmetic(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var context = new CompilationContext();

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];

            if (CSharpCallIntrinsicReader.TryGetCallMethod(instruction, out var method) &&
                method.DeclaringType == typeof(NativeArithmetic))
            {
                var intrinsicInstruction = CreateArithmeticInstruction(method);
                if (intrinsicInstruction != null)
                {
                    context.NewInstructions.Add(intrinsicInstruction);
                    continue;
                }
            }

            context.NewInstructions.Add(instruction);
        }

        var optimizedInstructions = ApplyArithmeticPeepholes(context.NewInstructions);

        var result = new AbstractIR();
        result.AppendInstructions(optimizedInstructions);
        return result;
    }

    private static IReadOnlyList<Instruction> ApplyArithmeticPeepholes(IReadOnlyList<Instruction> instructions)
    {
        var optimized = instructions.ToList();
        var changed = true;

        while (changed)
        {
            changed = false;
            for (var i = 0; i < optimized.Count; i++)
            {
                if (TryFoldAddMulReassociation(optimized, i))
                {
                    changed = true;
                    break;
                }

                if (TrySimplifyIdentity(optimized, i))
                {
                    changed = true;
                    break;
                }

                if (TryCanonicalizeCommutativeOperands(optimized, i))
                {
                    changed = true;
                    break;
                }
            }
        }

        return optimized;
    }

    private static bool TrySimplifyIdentity(List<Instruction> instructions, int index)
    {
        if (index + 2 >= instructions.Count)
            return false;

        var left = instructions[index];
        var right = instructions[index + 1];
        var op = instructions[index + 2];

        if (!TryGetArithmeticIntrinsic(op, out var symbol, out var suffix))
            return false;

        if (symbol == BuiltinIntrinsicSymbols.Arithmetic.Subtract &&
            IsSingleValueProducer(left) &&
            TryGetNumericConstant(right, out var rightValue) &&
            IsZero(rightValue))
            return Replace3With1(instructions, index, left);

        if (symbol == BuiltinIntrinsicSymbols.Arithmetic.Divide &&
            IsSingleValueProducer(left) &&
            TryGetNumericConstant(right, out rightValue) &&
            IsOne(rightValue))
            return Replace3With1(instructions, index, left);

        if (symbol == BuiltinIntrinsicSymbols.Arithmetic.Add)
        {
            if (TryGetNumericConstant(left, out var leftValue) && IsZero(leftValue) && IsSingleValueProducer(right))
                return Replace3With1(instructions, index, right);

            if (TryGetNumericConstant(right, out rightValue) && IsZero(rightValue) && IsSingleValueProducer(left))
                return Replace3With1(instructions, index, left);
        }

        if (symbol == BuiltinIntrinsicSymbols.Arithmetic.Multiply)
        {
            if (TryGetNumericConstant(left, out var leftValue))
            {
                if (IsOne(leftValue) && IsSingleValueProducer(right))
                    return Replace3With1(instructions, index, right);
                if (IsIntegerSuffix(suffix) && IsZero(leftValue))
                    return Replace3With1(instructions, index, new Instruction(UOpCode.Push, [GetTypedZero(suffix)]));
            }

            if (TryGetNumericConstant(right, out rightValue))
            {
                if (IsOne(rightValue) && IsSingleValueProducer(left))
                    return Replace3With1(instructions, index, left);
                if (IsIntegerSuffix(suffix) && IsZero(rightValue))
                    return Replace3With1(instructions, index, new Instruction(UOpCode.Push, [GetTypedZero(suffix)]));
            }
        }

        return false;
    }

    private static bool TryFoldAddMulReassociation(List<Instruction> instructions, int index)
    {
        if (index + 4 >= instructions.Count)
            return false;

        var x = instructions[index];
        var c1 = instructions[index + 1];
        var op1 = instructions[index + 2];
        var c2 = instructions[index + 3];
        var op2 = instructions[index + 4];

        if (!IsSingleValueProducer(x) ||
            !TryGetArithmeticIntrinsic(op1, out var symbol1, out var suffix1) ||
            !TryGetArithmeticIntrinsic(op2, out var symbol2, out var suffix2) ||
            symbol1 != symbol2 ||
            suffix1 != suffix2 ||
            !IsIntegerSuffix(suffix1) ||
            !TryGetNumericConstant(c1, out var v1) ||
            !TryGetNumericConstant(c2, out var v2))
            return false;

        object? combined = null;
        if (symbol1 == BuiltinIntrinsicSymbols.Arithmetic.Add)
            combined = CombineIntegerConstants(v1, v2, false, suffix1);
        else if (symbol1 == BuiltinIntrinsicSymbols.Arithmetic.Multiply)
            combined = CombineIntegerConstants(v1, v2, true, suffix1);

        if (combined is null)
            return false;

        instructions[index + 1] = new Instruction(UOpCode.Push, [combined]);
        instructions.RemoveAt(index + 3);
        instructions.RemoveAt(index + 3);
        return true;
    }

    private static bool TryCanonicalizeCommutativeOperands(List<Instruction> instructions, int index)
    {
        if (index + 2 >= instructions.Count)
            return false;

        var left = instructions[index];
        var right = instructions[index + 1];
        var op = instructions[index + 2];
        if (!TryGetArithmeticIntrinsic(op, out var symbol, out _) ||
            symbol != BuiltinIntrinsicSymbols.Arithmetic.Add && symbol != BuiltinIntrinsicSymbols.Arithmetic.Multiply)
            return false;

        if (!TryGetNumericConstant(left, out _) || TryGetNumericConstant(right, out _) || !IsSingleValueProducer(right))
            return false;

        instructions[index] = right;
        instructions[index + 1] = left;
        return true;
    }


    private static bool IsSingleValueProducer(Instruction instruction)
    {
        if (instruction.UOpCode == UOpCode.Push)
            return true;

        return BuiltinIntrinsicInstruction.Is(instruction, BuiltinIntrinsicSymbols.Storage.LoadLocal) ||
               BuiltinIntrinsicInstruction.Is(instruction, BuiltinIntrinsicSymbols.Storage.LoadLocalRef) ||
               instruction.UOpCode == UOpCode.Intrinsic &&
               instruction.Operands.Count > 0 &&
               instruction.Operands[0] is string intrinsicName &&
               intrinsicName.StartsWith("ldloc", StringComparison.Ordinal);
    }

    private static bool Replace3With1(List<Instruction> instructions, int index, Instruction replacement)
    {
        instructions[index] = replacement;
        instructions.RemoveAt(index + 1);
        instructions.RemoveAt(index + 1);
        return true;
    }

    private static bool TryGetArithmeticIntrinsic(Instruction instruction, out IntrinsicSymbol symbol, out string suffix)
    {
        symbol = default;
        suffix = string.Empty;
        if (!BuiltinIntrinsicInstruction.TryGetInvocation(instruction, out var invocation) ||
            !TryGetArithmeticSuffix(invocation, out suffix))
            return false;

        symbol = invocation.Symbol;
        return true;
    }

    private static bool TryGetNumericConstant(Instruction instruction, out object value)
    {
        value = null!;
        if (instruction.UOpCode != UOpCode.Push || instruction.Operands.Count != 1)
            return false;

        value = instruction.Operands[0];
        return value is int or long or float or double or decimal;
    }

    private static bool IsIntegerSuffix(string suffix) => suffix is "i32" or "i64";

    private static bool IsZero(object value) => value switch
    {
        int i => i == 0,
        long l => l == 0,
        float f => f == 0f,
        double d => d == 0d,
        decimal m => m == 0m,
        _ => false
    };

    private static bool IsOne(object value) => value switch
    {
        int i => i == 1,
        long l => l == 1,
        float f => Math.Abs(f - 1f) < 1e-9,
        double d => Math.Abs(d - 1d) < 1e-9,
        decimal m => m == 1m,
        _ => false
    };

    private static object GetTypedZero(string suffix) => suffix switch
    {
        "i32" => 0,
        "i64" => 0L,
        _ => 0
    };

    private static object? CombineIntegerConstants(object left, object right, bool isMultiply, string suffix)
    {
        if (suffix == "i32" && left is int li && right is int ri)
            return isMultiply ? li * ri : li + ri;
        if (suffix == "i64" && left is long ll && right is long rl)
            return isMultiply ? ll * rl : ll + rl;
        return null;
    }

    private static bool TryGetArithmeticSuffix(IntrinsicInvocation invocation, out string suffix)
    {
        suffix = string.Empty;
        if (!IsArithmeticSymbol(invocation.Symbol) || invocation.TypeArguments.Count != 1)
            return false;

        return TryMapTypeToSuffix(invocation.TypeArguments[0].RuntimeType, out suffix);
    }

    private static Instruction? CreateArithmeticInstruction(MethodInfo method)
    {
        if (!TryGetArithmeticSymbol(method, out var symbol, out var runtimeType))
            return null;

        return BuiltinIntrinsicInstruction.Create(symbol, runtimeType);
    }

    private static bool TryGetArithmeticSymbol(MethodInfo method, out IntrinsicSymbol symbol, out Type runtimeType)
    {
        symbol = default;
        runtimeType = null!;

        if (method.IsGenericMethod)
        {
            var genericType = method.GetGenericArguments()[0];
            if (!TryGetArithmeticSymbol(method.Name, out symbol))
                return false;

            runtimeType = genericType;
            return TryMapTypeToSuffix(runtimeType, out _);
        }

        if (!method.Name.EndsWith("Decimal", StringComparison.Ordinal) ||
            !TryGetArithmeticSymbol(method.Name.Replace("Decimal", string.Empty, StringComparison.Ordinal), out symbol))
            return false;

        runtimeType = typeof(decimal);
        return true;
    }

    private static bool TryGetArithmeticSymbol(string methodName, out IntrinsicSymbol symbol)
    {
        symbol = default;

        if (methodName == nameof(NativeArithmetic.Add))
        {
            symbol = BuiltinIntrinsicSymbols.Arithmetic.Add;
            return true;
        }

        if (methodName == nameof(NativeArithmetic.Subtract))
        {
            symbol = BuiltinIntrinsicSymbols.Arithmetic.Subtract;
            return true;
        }

        if (methodName == nameof(NativeArithmetic.Multiply))
        {
            symbol = BuiltinIntrinsicSymbols.Arithmetic.Multiply;
            return true;
        }

        if (methodName == nameof(NativeArithmetic.Divide))
        {
            symbol = BuiltinIntrinsicSymbols.Arithmetic.Divide;
            return true;
        }

        return false;
    }

    private static bool IsArithmeticSymbol(IntrinsicSymbol symbol) =>
        symbol == BuiltinIntrinsicSymbols.Arithmetic.Add ||
        symbol == BuiltinIntrinsicSymbols.Arithmetic.Subtract ||
        symbol == BuiltinIntrinsicSymbols.Arithmetic.Multiply ||
        symbol == BuiltinIntrinsicSymbols.Arithmetic.Divide;

    private static bool TryMapTypeToSuffix(Type runtimeType, out string suffix)
    {
        suffix = string.Empty;

        if (runtimeType == typeof(int))
        {
            suffix = "i32";
            return true;
        }

        if (runtimeType == typeof(long))
        {
            suffix = "i64";
            return true;
        }

        if (runtimeType == typeof(float))
        {
            suffix = "f32";
            return true;
        }

        if (runtimeType == typeof(double))
        {
            suffix = "f64";
            return true;
        }

        if (runtimeType == typeof(decimal))
        {
            suffix = "decimal";
            return true;
        }

        return false;
    }

    private class CompilationContext
    {
        public List<Instruction> NewInstructions { get; } = new();
    }
}