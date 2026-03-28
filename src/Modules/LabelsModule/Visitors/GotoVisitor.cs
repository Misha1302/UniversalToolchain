namespace LabelsModule.Visitors;

public class GotoVisitor(LabelsSharedData labelsSharedData) : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Goto")) return;

        var name = data.Node.Children[0].Text;
        var method = new AbstractMethodImpl(
            $"Goto_!Intrinsic_{name}",
            (il, _) => il.Jmp(labelsSharedData.GetIdByName(name))
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}