using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using ExceptionsManager;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using UniversalToolchain.Intrinsics.Legacy;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslCompiler
{
    private readonly BasicCoreImpl<DialectDefinitionSlice> _core;

    public DialectDslCompiler()
        : this(CreateDefaultFrontendModule())
    {
    }

    public DialectDslCompiler(DialectDslFrontendModule frontendModule)
    {
        if (frontendModule == null)
            Thrower.ArgumentNull(nameof(frontendModule));

        var compiler = new DialectDefinitionSliceCompiler();

        _core = new BasicCoreImpl<DialectDefinitionSlice>(
            () => new BasicLexerImpl(new LexerConfiguration([])),
            () => new BasicParserImpl(new ParserConfiguration([])),
            () => new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([])),
            CreateAbstractMethodsTranslator,
            () => compiler,
            () => new DialectDefinitionSliceExecutor(),
            [frontendModule],
            [],
            []);
    }

    public DialectDefinitionSlice Compile(string sourceText) => _core.GetExecutable(sourceText);

    private static DialectDslFrontendModule CreateDefaultFrontendModule() => DialectDslStandaloneComposition.CreateFrontendModule();

    private static IAbstractMethodsTranslator CreateAbstractMethodsTranslator()
    {
        return new BytecodeToAbstractIrConverterImpl(
            new LegacyIntrinsicDecoder(),
            CreateTypeStackProcessor());
    }

    private static IIntrinsicTypeStackProcessor CreateTypeStackProcessor()
    {
        var catalog = new IntrinsicCatalogBuilder().Build(CreateDescriptorProviders());
        return new IntrinsicTypeStackProcessor(catalog, new IntrinsicTypeResolutionContext());
    }

    private static IIntrinsicDescriptorProvider[] CreateDescriptorProviders()
    {
        return
        [
            new ArithmeticIntrinsicDescriptorProvider(),
            new ComparisonIntrinsicDescriptorProvider(),
            new BooleanIntrinsicDescriptorProvider(),
            new StorageIntrinsicDescriptorProvider(),
            new CoreIntrinsicDescriptorProvider(new MethodCallTypeSemanticsResolver())
        ];
    }
}
