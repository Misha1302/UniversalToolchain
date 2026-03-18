using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.Core;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using ExceptionsManager;

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
        {
            Thrower.ArgumentNull(nameof(frontendModule));
        }

        var compiler = new DialectDefinitionSliceCompiler();

        _core = new BasicCoreImpl<DialectDefinitionSlice>(
            () => new BasicLexerImpl(new LexerConfiguration([])),
            () => new BasicParserImpl(new ParserConfiguration([])),
            () => new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([])),
            () => new BytecodeToAbstractIrConverterImpl(),
            () => compiler,
            () => new DialectDefinitionSliceExecutor(),
            [frontendModule],
            [],
            []);
    }

    public DialectDefinitionSlice Compile(string sourceText)
    {
        return _core.GetExecutable(sourceText);
    }

    private static DialectDslFrontendModule CreateDefaultFrontendModule()
    {
        var registry = new DialectDslRegistry(
            [
                new UseModulesDialectDirectiveFeature(),
                new ExcludeModulesDialectDirectiveFeature(),
                new RequiresModulesDialectDirectiveFeature(),
                new BeforeModulesDialectDirectiveFeature(),
                new AfterModulesDialectDirectiveFeature(),
                new BackendDialectDirectiveFeature(),
                new AllowIntrinsicDialectDirectiveFeature(),
                new ForbidIntrinsicDialectDirectiveFeature(),
                new EnableOptimizerDialectDirectiveFeature(),
                new DisableOptimizerDialectDirectiveFeature(),
                new SecurityDialectDirectiveFeature(),
                new CapabilityDialectDirectiveFeature()
            ],
            [
                new UseExcludeConflictDocumentValidationRule()
            ]);

        return new DialectDslFrontendModule(registry);
    }
}
