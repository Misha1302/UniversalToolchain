using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslCompiler
{
    private readonly BasicCoreImpl<DialectDefinitionSlice> _core;

    public DialectDslCompiler()
    {
        _core = CreateCore();
    }

    public DialectDefinitionSlice Compile(string sourceText)
    {
        return _core.GetExecutable(sourceText);
    }

    private static BasicCoreImpl<DialectDefinitionSlice> CreateCore()
    {
        return new BasicCoreImpl<DialectDefinitionSlice>(
            CreateLexer,
            CreateParser,
            CreateAstTranslator,
            CreateBytecodeToAirConverter,
            CreateSliceCompiler,
            () => new DialectDefinitionSliceExecutor(),
            [CreateFrontendModule()],
            [],
            []);
    }

    private static IFrontendCoreModule CreateFrontendModule()
    {
        return new DialectDslFrontendModule();
    }

    private static ILexer CreateLexer()
    {
        return new BasicLexerImpl(new LexerConfiguration([]));
    }

    private static IParser CreateParser()
    {
        return new BasicParserImpl(new ParserConfiguration([]));
    }

    private static IAstToBytecodeTranslator CreateAstTranslator()
    {
        return new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([]));
    }

    private static IAbstractMethodsTranslator CreateBytecodeToAirConverter()
    {
        return new BytecodeToAbstractIrConverterImpl();
    }

    private static IAbstractIrCompiler<DialectDefinitionSlice> CreateSliceCompiler()
    {
        return new DialectDefinitionSliceCompiler();
    }
}
