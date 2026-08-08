using CommonExceptions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDefinitionTranslationErrors
{
    public static void Fail(string message, DialectSourceLocation? location)
    {
        if (location is { } source)
        {
            ToolchainThrower.Parser(
                message,
                new SourceLocation
                {
                    Line = source.Line,
                    Column = source.Column
                });
        }
        else
        {
            ToolchainThrower.Parser(message);
        }

        Thrower.InvalidOpEx("Unreachable dialect translation error path.");
    }
}
