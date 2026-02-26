using BasicCore.Attributes;
using BasicCore.Contracts;

namespace ParametersSetterModule;

[AutoRegisterService]
public class ParametersSetterModuleImpl : IFrontendCoreModule
{
    // public void InitLexer(ILexer lexer)
    // {
    //     lexer.Configuration.TryAddPattern(new LexemePattern(@"\(", LexemeType.CreateOrGet("OpenPar")));
    //     lexer.Configuration.TryAddPattern(new LexemePattern(@"\)", LexemeType.CreateOrGet("ClosePar")));
    // }
    //
    // public void InitParser(IParser parser)
    // {
    //     parser.Configuration.NodeCreators.Add(-100_000f, new ParametersSetNodeCreator());
    // }
    //
    // public AstNode ProcessAst(AstNode node)
    // {
    //     foreach (var child in node.Children)
    //     {
    //         ProcessAst(child);
    //     }
    //
    //     if (node.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("DefineParameter"))
    //     {
    //         node.
    //     }
    // }
}