using BasicCore;
using BasicCore.TranslatorWrapper;
using DynamicMethodWrapper;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;

namespace BytecodeDynamicMethodsCompiler;

public class AbstractMethodsStubImpl : IAbstractMethodsCompiler<IAbstractIR>
{
    public IAbstractIR Compile(Bytecode bytecode)
    {
        var ir = new AbstractIR();
        var typesStack = new List<Type>();
        foreach (var instruction in bytecode.Instructions)
        foreach (var op in instruction.Ops)
        foreach (var convertable in op.Value)
        {
            var context = new IAbstractMethodConvertable.Context(typesStack);
            var air = convertable.GetAbstractIR(context);
            var returnType = convertable.GetReturnType(context);

            ir.AppendInstructions(air);

            for (var i = 0; i < convertable.ParamsCount; i++)
                typesStack.RemoveAt(typesStack.Count - 1);

            if (returnType != typeof(void))
                typesStack.Add(returnType);
        }
        return ir;
    }
}