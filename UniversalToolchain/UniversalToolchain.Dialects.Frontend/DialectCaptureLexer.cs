using BasicCore.LexerWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectCaptureLexer : ILexer
{
    private readonly ILexer _inner;

    public DialectCaptureLexer(ILexer inner)
    {
        if (inner == null)
            Thrower.ArgumentNull(nameof(inner));

        _inner = inner;
    }

    public LexerConfiguration Configuration => _inner.Configuration;

    public List<LexemeValue> Lexemize(string code)
    {
        var tokens = _inner.Lexemize(code);
        DialectCompilationTokenContext.Set(tokens);
        return tokens;
    }
}
