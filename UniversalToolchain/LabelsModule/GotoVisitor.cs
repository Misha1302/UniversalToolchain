using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using UniversalIntermediateRepresentation;

namespace LabelsModule;

public class GotoVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Goto")) return;

        var name = data.Node.Children[0].Text;
        var method = new AbstractMethodImpl(
            $"Goto_!Intrinsic_{name}",
            0,
            (il, _) => il.Jmp(Guid.Parse(name)),
            _ => typeof(void)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}