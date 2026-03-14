namespace BasicCore.ParserWrapper;

public interface IParser
{
    ParserConfiguration Configuration { get; }
    AstNode Parse(List<LexemeValue> lexemes);
    public void ParseScope(AstNode scope, List<IAstNodeCreator> creators, Predicate<AstNode> needToVisit);
}