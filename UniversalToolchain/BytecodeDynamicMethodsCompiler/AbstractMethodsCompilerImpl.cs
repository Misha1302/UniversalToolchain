using System.Reflection;
using System.Reflection.Emit;
using AbstractIrExtensions;
using BasicCore;
using DotnetAirHelper;
using DotnetHelper;
using ExceptionsManager;
using GrEmit;
using IntermediateRepresentationAbstractions;
using ListExtensions;
using ObjectExtensions;

namespace BytecodeDynamicMethodsCompiler;

public class AbstractMethodsCompilerImpl : IAbstractIrCompiler<DynamicMethod>
{
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

    public DynamicMethod Compile(IAbstractIR air, OrderedDictionary<string, Type> parameters)
    {
        var returnType = GetReturnType(air);
        var argsTypes = parameters.Select(x => x.Value).ToArray();
        var paramsIndices = parameters.ToDictionary(y => y.Key, x => parameters.IndexOf(x.Key));
        var method = new DynamicMethod("main", returnType, argsTypes);
        using var il = new GroboIL(method);
        var data = new CompilationData(il, [], paramsIndices);

        InitializeLabels(data, air);

        var typesStack = new List<Type>();
        var labelStacks = new Dictionary<Guid, List<Type>>();

        foreach (var instruction in air.Instructions)
            CompileInstruction(data, instruction, typesStack, labelStacks);


        var type = typesStack.Last();
        if (type.IsValueType && !returnType.IsValueType)
            il.Box(type);
        il.Ret();


        return method;
    }

    private static Type GetReturnType(IAbstractIR air)
    {
        var stack = new List<Type>();
        var labelStacks = new Dictionary<Guid, List<Type>>();

        foreach (var instruction in air.Instructions)
        {
            // сначала восстанавливаем стек, если это label
            if (instruction.UOpCode == UOpCode.Label)
            {
                var labelId = instruction.Operands[0].Get<Guid>();
                if (labelStacks.TryGetValue(labelId, out var saved))
                {
                    stack.Clear();
                    stack.AddRange(saved);
                }
            }

            // симуляция инструкции
            instruction.ManipulateTypesStack(stack, AirTypes.ProcessTypesIntrinsic);

            // обработка ветвлений
            ProcessBranchingStack(instruction, stack, labelStacks);
        }

        return stack.Count > 0 ? stack[^1] : typeof(void);
    }

    private static void ProcessBranchingStack(
        Instruction instruction,
        List<Type> stack,
        Dictionary<Guid, List<Type>> labelStacks
    )
    {
        if (!instruction.UOpCode.IsAnyJump())
            return;

        var labelId = instruction.Operands[0].Get<Guid>();

        // JmpIf / JmpIfNot уже съели condition в ManipulateTypesStack
        labelStacks[labelId] = new List<Type>(stack);
    }


    private void InitializeLabels(CompilationData data, IAbstractIR bytecode)
    {
        var typesStack = new List<Type>();
        foreach (var instruction in bytecode.Instructions)
        {
            if (instruction.UOpCode == UOpCode.Label)
            {
                var id = instruction.Operands[0].Get<Guid>();
                data.InstructionLabels.Add((id, data.Il.DefineLabel($"Instruction {id}")));
            }

            instruction.ManipulateTypesStack(typesStack, AirTypes.ProcessTypesIntrinsic);
        }
    }


    private void CompileInstruction(
        CompilationData data,
        Instruction instruction,
        List<Type> stack,
        Dictionary<Guid, List<Type>> labelStacks
    )
    {
        // TODO: add stack tree in branching
        // TODO: this is necessary 'cause parallel blocks are able to leave >= 1 values on the stack 
        if (instruction.UOpCode == UOpCode.Nop)
        {
            data.Il.Nop();
        }
        else if (instruction.UOpCode == UOpCode.Push)
        {
            var obj = instruction.Operands[0];
            PushValue(data, obj);
            stack.Push(obj.GetType());
        }
        else if (instruction.UOpCode == UOpCode.Drop)
        {
            data.Il.Pop();
            stack.Pop();
        }
        else if (instruction.UOpCode == UOpCode.Jmp)
        {
            var labelId = instruction.Operands[0].Get<Guid>();

            // фиксируем стек для точки входа в label
            labelStacks[labelId] = new List<Type>(stack);

            data.Il.Br(
                data.InstructionLabels.First(x => x.id == labelId).label
            );
        }
        else if (instruction.UOpCode == UOpCode.JmpIf)
        {
            // условие съедается
            stack.Pop();

            var labelId = instruction.Operands[0].Get<Guid>();

            // стек для ветки прыжка
            labelStacks[labelId] = new List<Type>(stack);

            data.Il.Brtrue(
                data.InstructionLabels.First(x => x.id == labelId).label
            );
        }
        else if (instruction.UOpCode == UOpCode.JmpIfNot)
        {
            // условие съедается
            stack.Pop();

            var labelId = instruction.Operands[0].Get<Guid>();

            // стек для ветки прыжка
            labelStacks[labelId] = new List<Type>(stack);

            data.Il.Brfalse(
                data.InstructionLabels.First(x => x.id == labelId).label
            );
        }
        else if (instruction.UOpCode == UOpCode.Label)
        {
            var labelId = instruction.Operands[0].Get<Guid>();

            if (labelStacks.TryGetValue(labelId, out var savedStack))
            {
                stack.Clear();
                stack.AddRange(savedStack);
            }

            data.Il.MarkLabel(
                data.InstructionLabels.First(x => x.id == labelId).label
            );
        }
        else if (instruction.UOpCode == UOpCode.Annotate)
        {
        }
        else if (instruction.UOpCode == UOpCode.Intrinsic)
        {
            CompileIntrinsic(instruction, data, stack);
        }
        else
        {
            Thrower.InvalidOpEx();
        }
    }

    private void PushValue(CompilationData data, object value)
    {
        var type = value.GetType();
        var constants = typeof(GlobalExecutionConstants<>).MakeGenericType(type);
        var loadMethod = constants.GetMethod(nameof(GlobalExecutionConstants<>.GetValue)).NotNull();
        var addMethod = constants.GetMethod(nameof(GlobalExecutionConstants<>.AddValue)).NotNull();

        var ind = addMethod.Invoke(null, [value]).NotNull().Get<int>();

        data.Il.Ldc_I4(ind);
        data.Il.Call(loadMethod);
    }

    private void CompileIntrinsic(Instruction instruction, CompilationData data, List<Type> stack)
    {
        Thrower.AssertAlways(instruction.UOpCode == UOpCode.Intrinsic);
        Thrower.AssertAlways(instruction.Operands[0] is string);

        var name = instruction.Operands[0].Get<string>();
        if (name == "call C#")
        {
            var method = instruction.Operands[1].Get<MethodInfo>();
            Thrower.AssertAlways(method.DeclaringType != null);

            var methodParams = method.GetParameters().Select(x => x.ParameterType).ToList();
            var stackTypes = stack.TakeLast(methodParams.Count).ToList();
            var targetTypes = GenericTypeResolver.GetParameterTypes(method, stackTypes).ToList();
            if (!method.IsStatic)
            {
                targetTypes.Insert(0, method.DeclaringType);
                methodParams.Insert(0, method.DeclaringType);
                stackTypes.Insert(0, method.DeclaringType);
            }
            CastValuesToTypes(data, targetTypes, stackTypes);
            method = GenericTypeResolver.MakeGenericMethod(method, targetTypes);
            data.Il.Call(method);

            for (var i = 0; i < targetTypes.Count; i++)
                stack.Pop();
            if (method.ReturnType != typeof(void))
                stack.Push(method.ReturnType);
        }
        else if (name == "call C# ctor")
        {
            var method = instruction.Operands[1].Get<ConstructorInfo>();
            Thrower.AssertAlways(method.DeclaringType != null);

            var targetTypes = method.GetParameters().Select(x => x.ParameterType).ToList();
            var stackTypes = stack.TakeLast(targetTypes.Count).ToList();
            CastValuesToTypes(data, targetTypes, stackTypes);
            data.Il.Newobj(method);

            for (var i = 0; i < targetTypes.Count; i++)
                stack.Pop();
            stack.Push(method.DeclaringType);
        }
        else if (name == "store_local")
        {
            // New intrinsic: store_local "varName", varType
            var varName = instruction.Operands[1].Get<string>();
            var varType = instruction.Operands[2].Get<Type>();

            if (data.ParametersIndices.TryGetValue(varName, out var argIndex))
            {
                data.Il.Starg(argIndex);
                stack.Pop();
            }
            else
            {
                // Get or create local variable
                if (!data.LocalVariables.TryGetValue(varName, out var local))
                {
                    local = data.Il.DeclareLocal(varType);
                    data.LocalVariables[varName] = local;
                }

                // Value should already be on the stack
                data.Il.Stloc(local);

                // Remove value from stack
                stack.Pop();
            }
        }
        else if (name == "load_local")
        {
            // New intrinsic: load_local "varName", varType
            var varName = instruction.Operands[1].Get<string>();
            var varType = instruction.Operands[2].Get<Type>();

            if (data.ParametersIndices.TryGetValue(varName, out var argIndex))
            {
                data.Il.Ldarg(argIndex);
                stack.Push(varType);
            }
            else
            {
                // Get local variable
                if (!data.LocalVariables.TryGetValue(varName, out var local))
                {
                    // If variable is not declared, create it with default value
                    local = data.Il.DeclareLocal(varType);
                    data.LocalVariables[varName] = local;

                    // Initialize with default value
                    data.Il.Ldloca(local);
                    if (varType.IsValueType)
                    {
                        data.Il.Initobj(varType);
                    }
                    else
                    {
                        data.Il.Ldnull();
                        data.Il.Stloc(local);
                    }
                }

                // Load variable value onto stack
                data.Il.Ldloc(local);
                stack.Push(varType);
            }
        }
        else if (name == "load_local_ref")
        {
            // New intrinsic: load_local_ref "varName", varType
            var varName = instruction.Operands[1].Get<string>();
            var varType = instruction.Operands[2].Get<Type>();

            // Get local variable
            if (!data.LocalVariables.TryGetValue(varName, out var local))
            {
                local = data.Il.DeclareLocal(varType);
                data.LocalVariables[varName] = local;

                // Initialize with default value
                data.Il.Ldloca(local);
                if (varType.IsValueType)
                {
                    data.Il.Initobj(varType);
                }
                else
                {
                    data.Il.Ldnull();
                    data.Il.Stloc(local);
                }
            }

            // Load variable address onto stack
            data.Il.Ldloca(local);
            stack.Push(varType.MakeByRefType());
        }
        else if (name == "load_bool")
        {
            var value = instruction.Operands[1].Get<bool>();
            data.Il.Ldc_I4(value ? 1 : 0);
            stack.Push(typeof(bool));
        }
        else if (name == "boolean_and")
        {
            // Стек: [int, int] -> [int]
            Thrower.AssertAlways(stack.Count >= 2);
            Thrower.AssertAlways(stack[^1] == typeof(bool) && stack[^2] == typeof(bool));

            data.Il.And();
            stack.Pop();
            stack.Pop();
            stack.Push(typeof(bool));
        }
        else if (name == "boolean_or")
        {
            // Стек: [int, int] -> [int]
            Thrower.AssertAlways(stack.Count >= 2);
            Thrower.AssertAlways(stack[^1] == typeof(bool) && stack[^2] == typeof(bool));

            data.Il.Or();
            stack.Pop();
            stack.Pop();
            stack.Push(typeof(bool));
        }
        else if (name == "boolean_not")
        {
            // Стек: [int] -> [int]
            Thrower.AssertAlways(stack.Count >= 1);
            Thrower.AssertAlways(stack[^1] == typeof(bool));

            // Логическое NOT через XOR с 1
            data.Il.Ldc_I4(1);
            data.Il.Xor();

            // Обновляем стек: удаляем старый int, добавляем результат
            stack.Pop();
            stack.Push(typeof(bool));
        }
        else if (name.StartsWith("add_") || name.StartsWith("sub_") ||
                 name.StartsWith("mul_") || name.StartsWith("div_"))
        {
            CompileArithmeticIntrinsic(name, data, stack);
        }
        else if (name is "load_i32" or "load_i64" or "load_f32" or "load_f64" or "load_decimal")
        {
            LoadNativeNumber(instruction, data, stack);
        }
        else
        {
            Thrower.InvalidOpEx();
        }
    }

    private void LoadNativeNumber(Instruction instruction, CompilationData data, List<Type> stack)
    {
        var name = instruction.Operands[0].Get<string>();
        var arg = instruction.Operands[1];
        if (name == "load_i32")
        {
            data.Il.Ldc_I4(arg.Get<int>());
            stack.Push(typeof(int));
        }
        else if (name == "load_i64")
        {
            data.Il.Ldc_I8(arg.Get<long>());
            stack.Push(typeof(long));
        }
        else if (name == "load_f32")
        {
            data.Il.Ldc_R4(arg.Get<float>());
            stack.Push(typeof(float));
        }
        else if (name == "load_f64")
        {
            data.Il.Ldc_R8(arg.Get<double>());
            stack.Push(typeof(double));
        }
        else if (name == "load_decimal")
        {
            var dec = arg.Get<decimal>();

            var bits = decimal.GetBits(dec);
            var sign = (bits[3] & 0x80000000) != 0;
            var scale = (byte)(bits[3] >> 16 & 0x7f);
            data.Il.Ldc_I4(bits[0]);
            data.Il.Ldc_I4(bits[1]);
            data.Il.Ldc_I4(bits[2]);
            data.Il.Ldc_I4(sign ? 1 : 0);
            data.Il.Ldc_I4(scale);
            var ctor = typeof(decimal).GetConstructor([typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte)]);
            data.Il.Newobj(ctor);

            stack.Push(typeof(decimal));
        }
        else
        {
            Thrower.InvalidOpEx($"Unknown native number loading {name}");
        }
    }

    private void CompileArithmeticIntrinsic(string name, CompilationData data, List<Type> stack)
    {
        var parts = name.Split('_');
        var operation = parts[0]; // "add", "sub", "mul", "div"
        var typeStr = parts[1]; // "i32", "i64", "f32", "f64", "decimal"

        // Проверяем, что на стеке есть два значения
        if (stack.Count < 2)
            Thrower.InvalidOpEx("Not enough values on stack for binary operation");

        // Для decimal используем вызовы методов Decimal
        if (typeStr == "decimal")
        {
            var methodName = operation switch
            {
                "add" => "Add",
                "sub" => "Subtract",
                "mul" => "Multiply",
                "div" => "Divide",
                _ => throw new NotSupportedException($"Unknown decimal operation: {operation}")
            };

            var method = typeof(decimal).GetMethod(methodName, [typeof(decimal), typeof(decimal)]);
            data.Il.Call(method);

            // Снимаем два аргумента, кладем один результат
            stack.Pop();
            stack.Pop();
            stack.Push(typeof(decimal));
        }
        else
        {
            // Для примитивных типов генерируем IL-инструкции
            var resultType = GetTypeFromString(typeStr);

            // Проверяем типы на стеке
            if (stack[^1] != resultType || stack[^2] != resultType)
                Thrower.InvalidOpEx($"Type mismatch for operation {name}");

            // Генерация IL-инструкции
            switch (operation)
            {
                case "add":
                    data.Il.Add();
                    break;
                case "sub":
                    data.Il.Sub();
                    break;
                case "mul":
                    data.Il.Mul();
                    break;
                case "div":
                    data.Il.Div(false);
                    break;
                default:
                    Thrower.InvalidOpEx($"Unknown operation: {operation}");
                    break;
            }

            // Обновляем стек: снимаем два значения, кладем один
            stack.Pop();
            stack.Pop();
            stack.Push(resultType);
        }
    }

    private static Type GetTypeFromString(string typeStr) => typeStr switch
    {
        "i32" => typeof(int),
        "i64" => typeof(long),
        "f32" => typeof(float),
        "f64" => typeof(double),
        "decimal" => typeof(decimal),
        _ => throw new NotSupportedException($"Unsupported type string: {typeStr}")
    };

    private void CastValuesToTypes(CompilationData data, IReadOnlyList<Type> targetTypes, IReadOnlyList<Type> stackTypes)
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
            locals[i] = data.Il.DeclareLocal(sourceType);
            data.Il.Stloc(locals[i]);
        }

        for (var i = 0; i < targetTypes.Count; i++)
        {
            var sourceType = stackTypes[i];
            var targetType = targetTypes[i];

            data.Il.Ldloc(locals[i]);
            EmitCast(data, sourceType, targetType);
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

    private static void EmitCast(CompilationData data, Type sourceType, Type targetType)
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
            data.Il.Box(sourceType);
            return;
        }

        if (!sourceType.IsValueType && !targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return;

        Thrower.InvalidOpEx($"Cannot cast {sourceType} to {targetType}");
    }

    private class CompilationData(GroboIL il, List<(Guid id, GroboIL.Label label)> instructionLabels, Dictionary<string, int> parametersIndices)
    {
        public Dictionary<string, int> ParametersIndices { get; } = parametersIndices;
        public Dictionary<string, GroboIL.Local> LocalVariables { get; } = new();
        public GroboIL Il { get; } = il;
        public List<(Guid id, GroboIL.Label label)> InstructionLabels { get; } = instructionLabels;
    }

    private static class GlobalExecutionConstants<T>
    {
        private static readonly List<T> _values = [];

        public static int AddValue(T value)
        {
            _values.Add(value);
            return _values.Count - 1;
        }

        public static T GetValue(int index) => _values[index];
    }
}