// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.LexerWrapper;

namespace BasicCore.ParserWrapper;

public interface IParser
{
    ParserConfiguration Configuration { get; }
    AstNode Parse(List<LexemeValue> lexemes);
    public void ParseScope(AstNode scope, List<IAstNodeCreator> creator, Predicate<AstNode> needToVisit);
}