using BasicCore.Binding.Symbols;
using BasicCore.ParserWrapper;

namespace BasicCore.Binding;

public abstract class BoundAstNode : AstNode
{
    protected BoundAstNode(AstNode source, Symbol symbol)
        : base(source.NodeType, source.LexemeValue, source.Children.ToList())
    {
        Symbol = symbol;
        foreach (var tag in source.CurrentTags)
            AddTag(tag);
    }

    public Symbol Symbol { get; }
}

public sealed class BoundLocalReference : BoundAstNode
{
    public BoundLocalReference(AstNode source, LocalVariableSymbol symbol) : base(source, symbol)
    {
    }
}

public sealed class BoundExternalReference : BoundAstNode
{
    public BoundExternalReference(AstNode source, Symbol symbol) : base(source, symbol)
    {
    }
}

public sealed class BoundAssignment : AstNode
{
    public BoundAssignment(AstNode source) : base(source.NodeType, source.LexemeValue, source.Children.ToList())
    {
        foreach (var tag in source.CurrentTags)
            AddTag(tag);
    }
}

public sealed class BoundCall : AstNode
{
    public BoundCall(AstNode source) : base(source.NodeType, source.LexemeValue, source.Children.ToList())
    {
        foreach (var tag in source.CurrentTags)
            AddTag(tag);
    }
}

public sealed class BoundBinaryOperator : AstNode
{
    public BoundBinaryOperator(AstNode source) : base(source.NodeType, source.LexemeValue, source.Children.ToList())
    {
        foreach (var tag in source.CurrentTags)
            AddTag(tag);
    }
}
