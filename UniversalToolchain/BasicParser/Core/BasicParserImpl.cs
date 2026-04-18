namespace BasicParser.Core;

public class BasicParserImpl(ParserConfiguration configuration) : IParser
{
    public BasicParserImpl() : this(new ParserConfiguration([]))
    {
    }

    public ParserConfiguration Configuration { get; } = configuration;

    public AstNode Parse(List<LexemeValue> lexemes)
    {
        var nodes = lexemes.Select(x => new AstNode(AstNodeType.CreateOrGet("Unknown"), x, [])).ToList();
        var root = new AstNode(AstNodeType.CreateOrGet("Program"), null, nodes);
        SetAstNodeTypes(root);
        ParseRoot(root);
        Thrower.AssertAlways(new TreeValidator().IsValidTree(root), "Tree is invalid");
        return root;
    }

    public void ParseScope(AstNode scope, List<IAstNodeCreator> creators, Predicate<AstNode> needToVisit)
    {
        foreach (var node in scope.Children)
            ParseScope(node, creators, needToVisit);

        for (var i = 0; i < scope.Children.Count; i++)
        {
            var child = scope.Children[i];
            if (
                !creators.Any(x =>
                    (x.NeedToVisitPredicate ?? needToVisit)(child) && x.TryCreateNode(scope, i)
                )
            ) continue;

            child.MarkAsParserHandled();

            if (i >= 0 && i < scope.Children.Count)
            {
                var changedNode = scope.Children[i];
                if (!ReferenceEquals(changedNode, scope))
                    ParseScope(changedNode, creators, needToVisit);
            }

            i = -1;
        }
    }

    private void SetAstNodeTypes(AstNode root)
    {
        foreach (var node in root.Children)
        {
            if (node.LexemeType is not null)
                node.NodeType = AstNodeType.CreateOrGet(node.LexemeType.GetName());
        }
    }

    public void ParseRoot(AstNode root)
    {
        foreach (var creator in Configuration.NodeCreators)
            ParseScope(root, creator.Value, node => !node.IsParserHandled());
    }
}
