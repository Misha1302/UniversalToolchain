namespace UniversalToolchain.ModuleContracts;

public sealed class LegacyAstVisitorLoweringAdapter : IAstNodeLowerer
{
    private readonly IAstVisitor _visitor;

    public LegacyAstVisitorLoweringAdapter(ModuleId moduleId, AstNodeKind nodeKind, IAstVisitor visitor)
    {
        ModuleId = moduleId;
        NodeKind = nodeKind;
        _visitor = visitor.ArgNotNull();
    }

    public ModuleId ModuleId { get; }

    public AstNodeKind NodeKind { get; }

    public LoweringResult Lower(AstNode node, AstNodeLoweringContext context)
    {
        node = node.ArgNotNull();
        context = context.ArgNotNull();

        _visitor.TryVisit(new BytecodeVisitorData(context.Translator, context.Bytecode, node));
        return new LoweringResult(context.Bytecode, []);
    }
}
