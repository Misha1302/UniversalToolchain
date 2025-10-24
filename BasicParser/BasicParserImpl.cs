// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

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
        var nodes = lexemes.Select(x => new AstNode(AstNodeType.CreateOrGet("Unknown"), x, null, [])).ToList();
        var root = new AstNode(AstNodeType.CreateOrGet("Scope"), null, null, nodes);
        SetAstNodeTypes(root);
        ParseScope(root);
        Thrower.AssertAlways(new TreeValidator().IsValidTree(root), "Tree is invalid");
        return root;
    }

    private void SetAstNodeTypes(AstNode root)
    {
        foreach (var node in root.Children)
            if (node.LexemeType is not null)
                node.NodeType = AstNodeType.CreateOrGet(node.LexemeType.GetName());
    }

    private void ParseScope(AstNode scope)
    {
        Thrower.AssertAlways(scope.NodeType == AstNodeType.Get("Scope"));

        foreach (var creator in Configuration.NodeCreators)
            while (creator.Value.TryCreateNode(scope))
            {
            }
    }

    public object Parse(object lexemes)
    {
        throw new NotImplementedException();
    }
}