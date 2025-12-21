namespace BasicCore.LexerWrapper;

public interface ILexer
{
    public LexerConfiguration Configuration { get; }
    List<LexemeValue> Lexemize(string code);
}