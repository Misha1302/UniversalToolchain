namespace CommonExceptions;

public sealed class LexerException : ToolchainException
{
    public LexerException(string message, SourceLocation location) : base(message)
    {
        Stage = "Lexer";
        Location = location;
    }

    public LexerException(string message, Exception inner) : base(message, inner)
    {
        Stage = "Lexer";
    }
}

public sealed class ParserException : ToolchainException
{
    public ParserException(string message) : base(message)
    {
        Stage = "Parser";
    }

    public ParserException(string message, SourceLocation location) : base(message)
    {
        Stage = "Parser";
        Location = location;
    }

    public ParserException(string message, Exception inner) : base(message, inner)
    {
        Stage = "Parser";
    }
}

public sealed class BindingException : ToolchainException
{
    public BindingException(string message) : base(message)
    {
        Stage = "Binding";
    }

    public BindingException(string message, Exception inner) : base(message, inner)
    {
        Stage = "Binding";
    }
}

public sealed class BytecodeGenerationException : ToolchainException
{
    public BytecodeGenerationException(string message) : base(message)
    {
        Stage = "Bytecode";
    }

    public BytecodeGenerationException(string message, Exception inner) : base(message, inner)
    {
        Stage = "Bytecode";
    }
}

public sealed class RuntimeExecutionException : ToolchainException
{
    public RuntimeExecutionException(string message) : base(message)
    {
        Stage = "Runtime";
    }

    public RuntimeExecutionException(string message, Exception inner) : base(message, inner)
    {
        Stage = "Runtime";
    }
}

public sealed class TypeSystemException : ToolchainException
{
    public TypeSystemException(string message) : base(message)
    {
        Stage = "TypeSystem";
    }

    public TypeSystemException(string message, Exception inner) : base(message, inner)
    {
        Stage = "TypeSystem";
    }
}

public sealed class ImportException : ToolchainException
{
    public ImportException(string message) : base(message)
    {
        Stage = "Import";
    }

    public ImportException(string message, Exception inner) : base(message, inner)
    {
        Stage = "Import";
    }
}

public sealed class InternalCompilerException : ToolchainException
{
    public InternalCompilerException(string message) : base(message)
    {
        Stage = "InternalCompiler";
    }

    public InternalCompilerException(string message, Exception inner) : base(message, inner)
    {
        Stage = "InternalCompiler";
    }
}