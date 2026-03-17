using BasicCore.LexerWrapper;
using CommonExceptions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDefinitionSliceParseErrors
{
    public static void Fail(string message, LexemeValue? token)
    {
        if (token == null)
        {
            WistThrower.Parser(message);
            Thrower.InvalidOpEx("Unreachable parser error path.");
        }

        WistThrower.Parser(
            message,
            new SourceLocation
            {
                Line = token.LineNumber,
                Column = token.CharNumber
            });

        Thrower.InvalidOpEx("Unreachable parser error path.");
    }
}
