using System.Data;
using System.Diagnostics;
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

    public DynamicMethod Compile(Bytecode bytecode)
    {
        var method = new DynamicMethod("main", typeof(object), []);
        using var il = new GroboIL(method);

        var typesStack = new List<Type>();
        foreach (var instruction in bytecode.Instructions)
        foreach (var op in instruction.Ops)
        foreach (var convertable in op.Value)
        {
            var context = new IAbstractMethodConvertable.Context(typesStack);
            var air = convertable.GetAbstractIR(context);
            var returnType = convertable.GetReturnType(context);

            CompileAir(new CompilationData(il, [], []), air);

            if (returnType != typeof(void))
                typesStack.Add(returnType);

            for (var i = 0; i < convertable.ParamsCount; i++)
                typesStack.RemoveAt(typesStack.Count - 1);
        }

        if (typesStack.Count == 0) il.Ldnull();
        if (typesStack.Count != 0)
        {
            il.Ldfld(typeof(Value).GetField("Data"));
        }
        il.Ret();
        
        Debug.WriteLine(il.GetILCode());
        
        return method;
    }


    private void CompileAir(CompilationData data, AbstractIR air)
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
                // TODO: add checks for stack
                // TODO: undirectional jump
                data.Il.Br(data.GetLabel(instruction.Operands[0].Get<Guid>()));
            }
            else if (instruction.OpCode == UOpCode.JmpIf)
            {
                // TODO: add checks for stack
                // TODO: undirectional jump
                data.Il.Brtrue(data.GetLabel(instruction.Operands[0].Get<Guid>()));
            }
            else if (instruction.OpCode == UOpCode.JmpIfNot)
            {
                // TODO: add checks for stack
                // TODO: undirectional jump
                data.Il.Brfalse(data.GetLabel(instruction.Operands[0].Get<Guid>()));
            }
            else if (instruction.OpCode == UOpCode.Label)
            {
                data.Il.MarkLabel(data.GetLabel(instruction.Operands[0].Get<Guid>()));
            }
            else if (instruction.OpCode == UOpCode.LoadIp)
            {
                PushValue(data, Value.Create(data.InstructionIndex));
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
                CompileIntrinsic(instruction, data);
            }
            else
            {
                Thrower.InvalidOpEx();
            }

            data.InstructionIndex++;
        }
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

    private void CompileIntrinsic(Instruction instruction, CompilationData data)
    {
        Thrower.AssertAlways(instruction.OpCode == UOpCode.Intrinsic);
        Thrower.AssertAlways(instruction.Operands[0].Data is string);

        var name = instruction.Operands[0].Get<string>();
        if (name == "call C#")
        {
            var method = instruction.Operands[1].Get<MethodInfo>();
            Thrower.AssertAlways(method.DeclaringType != null);

            var targetTypes = method.GetParameters().Select(x => x.ParameterType).ToList();
            if (!method.IsStatic) targetTypes.Insert(0, method.DeclaringType);
            CastValuesToTypes(
                data,
                targetTypes,
                !method.IsStatic && method.DeclaringType.IsValueType && method.IsVirtual
            );
            data.Il.Call(method);
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
                typeof(Value).GetMethod("Get").NotNull()
                    .MakeGenericMethod(targetTypes[i])
            );

            if (i == 0 && needLoadReference)
            {
                var loc = data.Il.DeclareLocal(targetTypes[i]);
                data.Il.Stloc(loc);
                data.Il.Ldloca(loc);
            }
        }
    }

    private record CompilationData(GroboIL Il, List<(Guid id, GroboIL.Local local)> Locals, List<(Guid id, GroboIL.Label label)> Labels)
    {
        public int InstructionIndex;


        public GroboIL.Local GetLocal(int index)
        {
            return Locals[index].local;
        }

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

        public void TryAddLabel(Guid id)
        {
            if (Labels.Any(x => x.id == id)) return;

            Labels.Add(
                (
                    id,
                    Il.DefineLabel(id.ToString(), false)
                )
            );
        }

        public GroboIL.Label GetLabel(Guid id)
        {
            TryAddLabel(id);
            return Labels.First(x => x.id == id).label;
        }
    }

    private static class GlobalExecutionConstants
    {
        private static readonly List<Value> _values = [];

        public static int AddValue(Value value)
        {
            _values.Add(value);
            return _values.Count - 1;
        }

        public static Value GetValue(int index)
        {
            return _values[index];
        }
    }
}