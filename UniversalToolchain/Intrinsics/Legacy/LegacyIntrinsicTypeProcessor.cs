using System.Reflection;
using DotnetHelper;
using ObjectExtensions;

namespace UniversalToolchain.Intrinsics.Legacy;

internal static class LegacyIntrinsicTypeProcessor
{
    public static void ProcessTypes(Instruction instruction, List<Type> stack)
    {
        if (instruction == null)
            Thrower.ArgumentNull(nameof(instruction));

        if (stack == null)
            Thrower.ArgumentNull(nameof(stack));

        if (instruction.UOpCode != UOpCode.Intrinsic)
            Thrower.InvalidOpEx("Instruction must be an intrinsic opcode.");

        if (!IntrinsicInstructionLegacyProjector.TryProject(instruction, out var projectedInstruction))
            Thrower.InvalidOpEx($"Unsupported intrinsic instruction payload: {instruction}");

        ProcessProjectedInstruction(projectedInstruction, stack);
    }

    private static void ProcessProjectedInstruction(Instruction instruction, List<Type> stack)
    {
        var name = instruction.Operands[0].Get<string>();

        if (name == "call C#")
        {
            ProcessTypesCallCSharp(instruction, stack);
            return;
        }

        if (name == "call C# ctor")
        {
            ProcessTypesCallCSharpCtor(instruction, stack);
            return;
        }

        if (name == "store_local")
        {
            ProcessTypesStoreLocal(instruction, stack);
            return;
        }

        if (name == "load_local")
        {
            ProcessTypesLoadLocal(instruction, stack);
            return;
        }

        if (name == "load_local_ref")
        {
            ProcessTypesLoadLocalRef(instruction, stack);
            return;
        }

        if (name == "load_external")
        {
            ProcessTypesLoadExternal(instruction, stack);
            return;
        }

        if (name == "store_external")
        {
            ProcessTypesStoreExternal(instruction, stack);
            return;
        }

        if (name == "load_bool")
        {
            ProcessTypesLoadBool(instruction, stack);
            return;
        }

        if (name == "boolean_and")
        {
            ProcessTypesBooleanAnd(instruction, stack);
            return;
        }

        if (name == "boolean_or")
        {
            ProcessTypesBooleanOr(instruction, stack);
            return;
        }

        if (name == "boolean_not")
        {
            ProcessTypesBooleanNot(instruction, stack);
            return;
        }

        if (name.StartsWith("load_", StringComparison.Ordinal))
        {
            ProcessTypesLoadNativeNumber(instruction, stack);
            return;
        }

        if (name.StartsWith("add_", StringComparison.Ordinal)
            || name.StartsWith("sub_", StringComparison.Ordinal)
            || name.StartsWith("mul_", StringComparison.Ordinal)
            || name.StartsWith("div_", StringComparison.Ordinal))
        {
            ProcessTypesArithmeticIntrinsic(instruction, stack);
            return;
        }

        if (name.StartsWith("cmp_", StringComparison.Ordinal))
        {
            ProcessTypesComparisonIntrinsic(instruction, stack);
            return;
        }

        Thrower.InvalidOpEx($"Unknown intrinsic '{name}'.");
    }

    private static void ProcessTypesCallCSharp(Instruction instruction, List<Type> stack)
    {
        var method = instruction.Operands[1].Get<MethodInfo>();
        Thrower.AssertAlways(method.DeclaringType != null, $"Method '{method}' must have a declaring type.");

        var parametersCount = method.GetParameters().Length;
        Thrower.AssertAlways(
            stack.Count >= parametersCount,
            $"Not enough values on stack for intrinsic '{instruction.Operands[0].Get<string>()}'.");

        var stackTypes = stack.TakeLast(parametersCount).ToList();
        var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
        if (!method.IsStatic)
        {
            targetTypes.Insert(0, method.DeclaringType);
        }

        method = GenericTypeResolver.MakeGenericMethod(method, targetTypes);

        Thrower.AssertAlways(
            stack.Count >= targetTypes.Count,
            $"Not enough values on stack for intrinsic '{instruction.Operands[0].Get<string>()}'.");

        PopMany(stack, targetTypes.Count);
        if (method.ReturnType != typeof(void))
            stack.Add(method.ReturnType);
    }

    private static void ProcessTypesCallCSharpCtor(Instruction instruction, List<Type> stack)
    {
        var ctor = instruction.Operands[1].Get<ConstructorInfo>();
        Thrower.AssertAlways(ctor.DeclaringType != null, $"Constructor '{ctor}' must have a declaring type.");

        var parametersCount = ctor.GetParameters().Length;
        Thrower.AssertAlways(
            stack.Count >= parametersCount,
            $"Not enough values on stack for intrinsic '{instruction.Operands[0].Get<string>()}'.");

        PopMany(stack, parametersCount);
        stack.Add(ctor.DeclaringType);
    }

    private static void ProcessTypesStoreLocal(Instruction instruction, List<Type> stack)
    {
        Thrower.AssertAlways(
            stack.Count >= 1,
            $"Not enough values on stack for intrinsic '{instruction.Operands[0].Get<string>()}'.");
        stack.RemoveAt(stack.Count - 1);
    }

    private static void ProcessTypesLoadLocal(Instruction instruction, List<Type> stack)
    {
        var varType = instruction.Operands[2].Get<Type>();
        stack.Add(varType);
    }

    private static void ProcessTypesLoadLocalRef(Instruction instruction, List<Type> stack)
    {
        var varType = instruction.Operands[2].Get<Type>();
        stack.Add(varType.MakeByRefType());
    }

    private static void ProcessTypesLoadExternal(Instruction instruction, List<Type> stack)
    {
        var varType = instruction.Operands[2].Get<Type>();
        stack.Add(varType);
    }

    private static void ProcessTypesStoreExternal(Instruction instruction, List<Type> stack)
    {
        Thrower.AssertAlways(
            stack.Count >= 1,
            $"Not enough values on stack for intrinsic '{instruction.Operands[0].Get<string>()}'.");
        stack.RemoveAt(stack.Count - 1);
    }

    private static void ProcessTypesLoadBool(Instruction instruction, List<Type> stack)
    {
        stack.Add(typeof(bool));
    }

    private static void ProcessTypesBooleanAnd(Instruction instruction, List<Type> stack)
    {
        EnsureBinaryBoolOperands(stack, instruction.Operands[0].Get<string>());
        PopTwoPush(stack, typeof(bool));
    }

    private static void ProcessTypesBooleanOr(Instruction instruction, List<Type> stack)
    {
        EnsureBinaryBoolOperands(stack, instruction.Operands[0].Get<string>());
        PopTwoPush(stack, typeof(bool));
    }

    private static void ProcessTypesBooleanNot(Instruction instruction, List<Type> stack)
    {
        Thrower.AssertAlways(
            stack.Count >= 1,
            $"Not enough values on stack for intrinsic '{instruction.Operands[0].Get<string>()}'.");
        Thrower.AssertAlways(
            stack[^1] == typeof(bool),
            $"Intrinsic '{instruction.Operands[0].Get<string>()}' requires a boolean operand.");

        stack.RemoveAt(stack.Count - 1);
        stack.Add(typeof(bool));
    }

    private static void ProcessTypesLoadNativeNumber(Instruction instruction, List<Type> stack)
    {
        var name = instruction.Operands[0].Get<string>();
        stack.Add(GetLoadIntrinsicType(name));
    }

    private static void ProcessTypesArithmeticIntrinsic(Instruction instruction, List<Type> stack)
    {
        var name = instruction.Operands[0].Get<string>();
        var (_, _, operandType) = ParseIntrinsicSignature(name);

        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for intrinsic '{name}'.");
        Thrower.AssertAlways(stack[^1] == operandType && stack[^2] == operandType, $"Type mismatch for intrinsic '{name}'.");

        PopTwoPush(stack, operandType);
    }

    private static void ProcessTypesComparisonIntrinsic(Instruction instruction, List<Type> stack)
    {
        var name = instruction.Operands[0].Get<string>();
        var (_, _, operandType) = ParseIntrinsicSignature(name);

        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for intrinsic '{name}'.");
        Thrower.AssertAlways(stack[^1] == operandType && stack[^2] == operandType, $"Type mismatch for intrinsic '{name}'.");

        PopTwoPush(stack, typeof(bool));
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

        return Thrower.InvalidOpEx<Type>($"Unsupported intrinsic type token: {typeStr}");
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
            "load_bool" => typeof(bool),
            _ => Thrower.InvalidOpEx<Type>($"Unknown load intrinsic '{name}'.")
        };
    }

    private static void EnsureBinaryBoolOperands(List<Type> stack, string intrinsicName)
    {
        Thrower.AssertAlways(stack.Count >= 2, $"Not enough values on stack for intrinsic '{intrinsicName}'.");
        Thrower.AssertAlways(
            stack[^1] == typeof(bool) && stack[^2] == typeof(bool),
            $"Intrinsic '{intrinsicName}' requires two boolean operands.");
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
        {
            stack.RemoveAt(stack.Count - 1);
        }
    }
}
