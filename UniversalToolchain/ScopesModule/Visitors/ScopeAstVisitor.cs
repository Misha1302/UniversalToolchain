namespace ScopesModule.Visitors;

public class ScopeAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Scope")) return;

        foreach (var child in data.Node.Children)
            data.AstToBytecodeTranslator.Translate(child);
    }
}