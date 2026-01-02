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
        "call C#",
        "call C# ctor",
        "store_local",
        "load_local",
        "load_local_ref",
        "load_i32",
        "load_i64",
        "load_f32",
        "load_f64"
    ];

    public DynamicMethod Compile(IAbstractIR air)
    {
        var method = new DynamicMethod("main", typeof(object), []);
        using var il = new GroboIL(method);
        var data = new CompilationData(il, []);
        InitializeLabels(data, air);

        var typesStack = new List<Type>();
        foreach (var instruction in air.Instructions)
        {
            CompileInstruction(data, instruction, typesStack);
        }

        if (typesStack.Count == 0)
            il.Ldnull();
        else if (typesStack[0].IsValueType)
            il.Box(typesStack[0]);
        il.Ret();

        return method;
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


    private void CompileInstruction(CompilationData data, Instruction instruction, List<Type> stack)
    {
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
            data.Il.Br(
                data.InstructionLabels.First(x => x.id == instruction.Operands[0].Get<Guid>()).label
            );
        }
        else if (instruction.UOpCode == UOpCode.JmpIf)
        {
            data.Il.Brtrue(
                data.InstructionLabels.First(x => x.id == instruction.Operands[0].Get<Guid>()).label
            );
            stack.Pop();
        }
        else if (instruction.UOpCode == UOpCode.JmpIfNot)
        {
            data.Il.Brfalse(
                data.InstructionLabels.First(x => x.id == instruction.Operands[0].Get<Guid>()).label
            );
            stack.Pop();
        }
        else if (instruction.UOpCode == UOpCode.Label)
        {
            data.Il.MarkLabel(data.InstructionLabels.First(x => x.id == instruction.Operands[0].Get<Guid>()).label);
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
            var stackTypes = stack.TakeLast(methodParams.Count).Reverse().ToList();
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
            var stackTypes = stack.TakeLast(targetTypes.Count).Reverse().ToList();
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
        else if (name == "load_local")
        {
            // New intrinsic: load_local "varName", varType
            var varName = instruction.Operands[1].Get<string>();
            var varType = instruction.Operands[2].Get<Type>();

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
        else if (name is "load_i32" or "load_i64" or "load_f32" or "load_f64")
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
        else
        {
            Thrower.InvalidOpEx($"Unknown native number loading {name}");
        }
    }


    private void CastValuesToTypes(CompilationData data, IReadOnlyList<Type> targetTypes, IReadOnlyList<Type> stackTypes)
    {
        var n = targetTypes.Count;
        var locals = new GroboIL.Local[n];
        for (var i = 0; i < locals.Length; i++)
        {
            var locType = targetTypes[locals.Length - 1 - i];
            if (stackTypes[i] != locType)
            {
                if (stackTypes[i].IsValueType && !locType.IsValueType)
                    data.Il.Box(stackTypes[i]);
                else Thrower.InvalidOpEx($"Cannot cast {stackTypes[i]} to {locType}");
            }

            locals[i] = data.Il.DeclareLocal(locType);
            data.Il.Stloc(locals[i]);
        }

        for (var i = locals.Length - 1; i >= 0; i--)
        {
            data.Il.Ldloc(locals[i]);
        }
    }

    private class CompilationData(GroboIL il, List<(Guid id, GroboIL.Label label)> instructionLabels)
    {
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