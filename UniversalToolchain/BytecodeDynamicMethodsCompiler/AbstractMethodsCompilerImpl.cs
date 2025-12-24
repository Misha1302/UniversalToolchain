using System.Reflection;
using System.Reflection.Emit;
using BasicCore;
using BasicCore.TranslatorWrapper;
using DynamicMethodWrapper;
using ExceptionsManager;
using GrEmit;
using UniversalIntermediateRepresentation;
using UOpCode = UniversalIntermediateRepresentation.OpCode;

namespace BytecodeDynamicMethodsCompiler;

public class AbstractMethodsCompilerImpl : IAbstractMethodsCompiler<DynamicMethod>
{
    private static readonly MethodInfo _valueCreator = typeof(Value).GetMethod("Create").NotNull();
    private static readonly MethodInfo _getMethod = typeof(Value).GetMethod("Get").NotNull();

    public DynamicMethod Compile(Bytecode bytecode)
    {
        var method = new DynamicMethod("main", typeof(object), []);
        using var il = new GroboIL(method);
        var data = new CompilationData(il, [], []);
        InitializeLabels(data, bytecode);

        var typesStack = new List<Type>();
        foreach (var instruction in bytecode.Instructions)
        foreach (var op in instruction.Ops)
        foreach (var convertable in op.Value)
        {
            var context = new IAbstractMethodConvertable.Context(typesStack);
            var air = convertable.GetAbstractIR(context);
            var returnType = convertable.GetReturnType(context);

            CompileAir(data, air, typesStack);

            for (var i = 0; i < convertable.ParamsCount; i++)
                typesStack.RemoveAt(typesStack.Count - 1);

            if (returnType != typeof(void))
                typesStack.Add(returnType);
        }

        if (typesStack.Count == 0) il.Ldnull();
        if (typesStack.Count != 0)
        {
            il.Ldfld(typeof(Value).GetField("Data"));
        }
        il.Ret();

        GlobalExecutionConstants.Initialize(data.Locals.Select((x, i) => (x.id, i)).ToDictionary());

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
            var returnType = convertable.GetReturnType(context);

            foreach (var label in air.Instructions.Where(x => x.OpCode == UOpCode.Label))
            {
                var id = label.Operands[0].Get<Guid>();
                data.InstructionLabels.Add((id, data.Il.DefineLabel($"Instruction {id}")));
            }

            for (var i = 0; i < convertable.ParamsCount; i++)
                typesStack.RemoveAt(typesStack.Count - 1);

            if (returnType != typeof(void))
                typesStack.Add(returnType);
        }
    }


    private void CompileAir(CompilationData data, AbstractIR air, List<Type> stack)
    {
        foreach (var instruction in air.Instructions)
        {
            if (instruction.OpCode == UOpCode.Nop)
            {
                data.Il.Nop();
            }
            else if (instruction.OpCode == UOpCode.Push)
            {
                PushValue(data, instruction.Operands[0]);
            }
            else if (instruction.OpCode == UOpCode.Drop)
            {
                data.DropLastLocal();
            }
            else if (instruction.OpCode == UOpCode.Jmp)
            {
                data.Il.Br(
                    data.InstructionLabels.First(x => x.id == instruction.Operands[0].Get<Guid>()).label
                );
            }
            else if (instruction.OpCode == UOpCode.JmpIf)
            {
                var loc2 = data.Il.DeclareLocal(typeof(Value));
                data.Il.Stloc(loc2);
                data.Il.Ldloca(loc2);
                data.Il.Call(_getMethod.MakeGenericMethod(typeof(bool)));
                data.Il.Brtrue(
                    data.InstructionLabels.First(x => x.id == instruction.Operands[0].Get<Guid>()).label
                );
            }
            else if (instruction.OpCode == UOpCode.JmpIfNot)
            {
                var loc2 = data.Il.DeclareLocal(typeof(Value));
                data.Il.Stloc(loc2);
                data.Il.Ldloca(loc2);
                data.Il.Call(_getMethod.MakeGenericMethod(typeof(bool)));
                data.Il.Brfalse(
                    data.InstructionLabels.First(x => x.id == instruction.Operands[0].Get<Guid>()).label
                );
            }
            else if (instruction.OpCode == UOpCode.Label)
            {
                data.Il.MarkLabel(data.InstructionLabels.First(x => x.id == instruction.Operands[0].Get<Guid>()).label);
            }
            else if (instruction.OpCode == UOpCode.StLoc)
            {
                data.Il.Stloc(data.GetLocal(instruction.Operands[0].Get<Guid>()));
            }
            else if (instruction.OpCode == UOpCode.LdLoc)
            {
                data.Il.Ldloc(data.GetLocal(instruction.Operands[0].Get<Guid>()));
            }
            else if (instruction.OpCode == UOpCode.Annotate)
            {
            }
            else if (instruction.OpCode == UOpCode.Intrinsic)
            {
                CompileIntrinsic(instruction, data, stack);
            }
            else
            {
                Thrower.InvalidOpEx();
            }
        }

        data.InstructionIndex++;
    }

    private void PushValue(CompilationData data, Value value)
    {
        var loadMethod = typeof(GlobalExecutionConstants)
            .GetMethod(nameof(GlobalExecutionConstants.GetValue))
            .NotNull();

        var ind = GlobalExecutionConstants.AddValue(value);
        data.Il.Ldc_I4(ind);
        data.Il.Call(loadMethod);
    }

    private void CompileIntrinsic(Instruction instruction, CompilationData data, List<Type> stack)
    {
        Thrower.AssertAlways(instruction.OpCode == UOpCode.Intrinsic);
        Thrower.AssertAlways(instruction.Operands[0].Data is string);

        var name = instruction.Operands[0].Get<string>();
        if (name == "call C#")
        {
            var method = instruction.Operands[1].Get<MethodInfo>();
            Thrower.AssertAlways(method.DeclaringType != null);

            // TODO: refactor this!

            // TODO: fix generics in parameters
            var targetTypes = GetParameterTypes(method, stack).ToList();
            if (!method.IsStatic) targetTypes.Insert(0, method.DeclaringType);
            CastValuesToTypes(
                data,
                targetTypes,
                !method.IsStatic && method.DeclaringType.IsValueType && method.IsVirtual
            );
            data.Il.Call(MakeGenericMethod(method, targetTypes));
            if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Value))
            {
                if (method.ReturnType.IsValueType)
                    data.Il.Box(method.ReturnType);
                data.Il.Call(_valueCreator);
            }
        }
        else if (name == "call C# ctor")
        {
            var method = instruction.Operands[1].Get<ConstructorInfo>();
            Thrower.AssertAlways(method.DeclaringType != null);

            var targetTypes = method.GetParameters().Select(x => x.ParameterType).ToList();
            CastValuesToTypes(
                data,
                targetTypes,
                !method.IsStatic && method.DeclaringType.IsValueType && method.IsVirtual
            );
            data.Il.Newobj(method);
            if (method.DeclaringType != typeof(Value))
            {
                if (method.DeclaringType.NotNull().IsValueType)
                    data.Il.Box(method.DeclaringType);
                data.Il.Call(_valueCreator);
            }
        }
        else
        {
            Thrower.InvalidOpEx();
        }
    }

    private IReadOnlyList<Type> GetParameterTypes(MethodInfo method, List<Type> stack)
    {
        var types = (List<Type>)[];
        var parameters = method.GetParameters();
        foreach (var parameter in parameters)
        {
            var targetType = parameter.ParameterType.ContainsGenericParameters
                ? MakeGenericType(parameter.ParameterType, stack.TakeLast(parameters.Length).Reverse().ToList())
                : parameter.ParameterType;
            types.Add(targetType);
        }
        return types;
    }


    private static Type MakeGenericType(Type parameterType, List<Type> sourceTypes)
    {
        var gArgs = parameterType.GetGenericArguments();
        if (!parameterType.IsGenericType)
            return sourceTypes[0];

        var genericTypes = gArgs
            .Select((x, i) => x.FullName == null ? sourceTypes[i] : x)
            .ToArray();

        return parameterType.GetGenericTypeDefinition().MakeGenericType(genericTypes);
    }

    private void CastValuesToTypes(CompilationData data, IReadOnlyList<Type> targetTypes, bool needLoadReference)
    {
        var n = targetTypes.Count;
        var locals = new GroboIL.Local[n];
        for (var i = 0; i < locals.Length; i++)
        {
            locals[i] = data.Il.DeclareLocal(typeof(Value));
            data.Il.Stloc(locals[i]);
        }
        for (var i = 0; i < locals.Length; i++)
        {
            data.Il.Ldloca(locals[locals.Length - 1 - i]);
            data.Il.Call(
                _getMethod.MakeGenericMethod(targetTypes[i])
            );

            if (i == 0 && needLoadReference)
            {
                var loc = data.Il.DeclareLocal(targetTypes[i]);
                data.Il.Stloc(loc);
                data.Il.Ldloca(loc);
            }
        }
    }

    private static MethodInfo MakeGenericMethod(MethodInfo call, List<Type> argTypes)
    {
        if (!call.ContainsGenericParameters) return call;

        var genericTypes = call.GetGenericArguments()
            .Select((x, i) => x.FullName == null ? argTypes[i] : x)
            .ToArray();

        return call.GetGenericMethodDefinition().MakeGenericMethod(genericTypes);
    }

    private record CompilationData(GroboIL Il, List<(Guid id, GroboIL.Local local)> Locals, List<(Guid id, GroboIL.Label label)> InstructionLabels)
    {
        public int InstructionIndex;


        public GroboIL.Local GetLocal(Guid id)
        {
            TryAddLocal(id);
            return Locals.First(x => x.id == id).local;
        }

        public void DropLastLocal()
        {
            Locals.RemoveAt(Locals.Count - 1);
        }

        public void TryAddLocal(Guid id)
        {
            if (Locals.Any(x => x.id == id)) return;

            Locals.Add(
                (
                    id,
                    Il.DeclareLocal(typeof(Value), id.ToString(), appendUniquePrefix: false)
                )
            );
        }
    }

    private static class GlobalExecutionConstants
    {
        private static Dictionary<Guid, int> _guidToLabelIndex = [];
        private static readonly List<Value> _values = [];

        public static void Initialize(Dictionary<Guid, int> guidToLabelIndex)
        {
            _guidToLabelIndex = guidToLabelIndex;
        }

        public static int AddValue(Value value)
        {
            _values.Add(value);
            return _values.Count - 1;
        }

        public static Value GetValue(int index)
        {
            return _values[index];
        }

        public static int ValueGuidToLabelIndex(Value label)
        {
            return _guidToLabelIndex[label.Get<Guid>()];
        }
    }
}