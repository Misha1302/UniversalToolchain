namespace BasicCodeTranslator.Visitors;

public sealed class StructuralScopeAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Scope"))
        {
            return;
        }

        if (data.Bytecode.Instructions.Count != data.InstructionCountBeforeVisit)
        {
            return;
        }

        foreach (var child in data.Node.Children)
        {
            data.AstToBytecodeTranslator.Translate(child);
        }
    }
}
