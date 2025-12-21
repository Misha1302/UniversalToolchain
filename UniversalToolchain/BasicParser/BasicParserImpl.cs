using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace BasicParser;

public class BasicParserImpl(ParserConfiguration configuration) : IParser
{
    public BasicParserImpl() : this(new ParserConfiguration([]))
    {
    }

    public ParserConfiguration Configuration { get; } = configuration;

    public AstNode Parse(List<LexemeValue> lexemes)
    {
        var nodes = lexemes.Select(x => new AstNode(AstNodeType.CreateOrGet("Unknown"), x, [])).ToList();
        var root = new AstNode(AstNodeType.CreateOrGet("Scope"), null, nodes);
        SetAstNodeTypes(root);
        ParseRoot(root);
        Thrower.AssertAlways(new TreeValidator().IsValidTree(root), "Tree is invalid");
        return root;
    }

    public void ParseScope(AstNode scope, List<IAstNodeCreator> creator, Predicate<AstNode> needToVisit)
    {
        foreach (var node in scope.Children)
            ParseScope(node, creator, needToVisit);

        for (var i = 0; i < scope.Children.Count; i++)
        {
            var child = scope.Children[i];
            if (!needToVisit(child)) continue;
            if (creator.All(x => !x.TryCreateNode(scope, i))) continue;

            child.MarkAsParserHandled();
            i = -1;
        }

        foreach (var node in scope.Children)
            ParseScope(node, creator, needToVisit);
    }

    private void SetAstNodeTypes(AstNode root)
    {
        foreach (var node in root.Children)
            if (node.LexemeType is not null)
                node.NodeType = AstNodeType.CreateOrGet(node.LexemeType.GetName());
    }

    public void ParseRoot(AstNode root)
    {
        foreach (var creator in Configuration.NodeCreators)
            ParseScope(root, creator.Value, node => !node.IsParserHandled());
    }
}