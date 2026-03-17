using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicLexer.Core;
using BasicCore.Registration;
using BasicCore.LexerWrapper;
using IntermediateRepresentationAbstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSliceCompiler : IAbstractIrCompiler<DialectDefinitionSlice>
{
    private readonly DialectDefinitionSliceParser _sliceParser = new();

    public DialectDefinitionSlice Compile(IAbstractIR air, CompilationInput input)
    {
        if (air == null)
        {
            Thrower.ArgumentNull(nameof(air));
        }

        if (input == null)
        {
            Thrower.ArgumentNull(nameof(input));
        }

        if (DialectCompilationTokenContext.TryTake(out var tokensFromPipeline))
        {
            return _sliceParser.Parse(tokensFromPipeline);
        }

        var lexer = CreateLexer();
        var tokens = lexer.Lexemize(input.SourceText);
        return _sliceParser.Parse(tokens);
    }

    private static ILexer CreateLexer()
    {
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        lexer.AddLexemes(DialectDslLexemeRegistry.Registrations);
        return lexer;
    }
}
