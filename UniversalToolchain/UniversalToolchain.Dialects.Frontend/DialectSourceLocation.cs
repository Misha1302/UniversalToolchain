using BasicCore.LexerWrapper;

namespace UniversalToolchain.Dialects.Frontend;

public readonly record struct DialectSourceLocation(
    int StartIndex,
    int Length,
    int Line,
    int Column)
{
    internal static DialectSourceLocation? From(LexemeValue? token)
    {
        if (token == null || token.StartIndex < 0)
            return null;
        return new DialectSourceLocation(
            token.StartIndex,
            token.Text.Length,
            token.LineNumber,
            token.CharNumber);
    }
}
