using System.Reflection;
using System.Reflection.Emit;
using AbstractIrExtensions;
using BasicCore;
using BasicCore.TranslatorWrapper;
using DotnetAirHelper;
using DotnetHelper;
using DynamicMethodWrapper;
using ExceptionsManager;
using GrEmit;
using IntermediateRepresentationAbstractions;
using ListExtensions;
using ObjectExtensions;

namespace BytecodeDynamicMethodsCompiler;

public class AbstractMethodsCompilerImpl : IAbstractMethodsCompiler<DynamicMethod>
{
    public DynamicMethod Compile(Bytecode bytecode)
    {
        var method = new DynamicMethod("main", typeof(object), []);
        using var il = new GroboIL(method);
        var data = new CompilationData(il, []);
        InitializeLabels(data, bytecode);

        var typesStack = new List<Type>();
        foreach (var instruction in bytecode.Instructions)
        foreach (var op in instruction.Ops)
        foreach (var convertable in op.Value)
        {
            var context = new IAbstractMethodConvertable.Context(typesStack);
            var air = convertable.GetAbstractIR(context);

            CompileAir(data, air, typesStack);
        }

        if (typesStack.Count == 0)
            il.Ldnull();
        else if (typesStack[0].IsValueType)
            il.Box(typesStack[0]);
        il.Ret();

        return method;
    }

    private void InitializeLabels(CompilationData data, Bytecode bytecode)
    {
        var typesStack = new List<Type>();
        foreach (var instruction in bytecode.Instructions)
        foreach (var op in instruction.Ops)
        foreach (var convertable in op.Value)
        {
            var context = new IAbstractMethodConvertable.Context(typesStack);
            var air = convertable.GetAbstractIR(context);

            foreach (var label in air.Instructions.Where(x => x.UOpCode == UOpCode.Label))
            {
                var id = label.Operands[0].Get<Guid>();
                data.InstructionLabels.Add((id, data.Il.DefineLabel($"Instruction {id}")));
            }

            air.ManipulateTypesStack(typesStack, AirTypes.ProcessTypesIntrinsic);
        }
    }


    private void CompileAir(CompilationData data, IAbstractIR air, List<Type> stack)
    {
        foreach (var instruction in air.Instructions)
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
        else
        {
            Thrower.InvalidOpEx();
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

    private record CompilationData(GroboIL Il, List<(Guid id, GroboIL.Label label)> InstructionLabels);

    private static class GlobalExecutionConstants<T>
    {
        private static readonly List<T> _values = [];

        public static int AddValue(T value)
        {
            _values.Add(value);
            return _values.Count - 1;
        }

        public static T GetValue(int index)
        {
            return _values[index];
        }
    }
}