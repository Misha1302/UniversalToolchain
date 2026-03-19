namespace CommonExceptions;

public static class WistThrower
{
    public static void Lexer(string message, SourceLocation location)
    {
        throw new LexerException(message, location);
    }

    public static void Parser(string message)
    {
        throw new ParserException(message);
    }

    public static void Parser(string message, SourceLocation location)
    {
        throw new ParserException(message, location);
    }

    public static void Import(string message)
    {
        throw new ImportException(message);
    }

    public static void Runtime(string message, Exception inner)
    {
        throw new RuntimeExecutionException(message, inner);
    }

    public static void InternalCompiler(string message)
    {
        throw new InternalCompilerException(message);
    }
}