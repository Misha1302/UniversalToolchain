using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslCompiler : IDisposable
{
    private readonly BasicCoreImpl<DialectDefinitionSlice> _core;
    private readonly ServiceProvider _serviceProvider;

    public DialectDslCompiler()
        : this(CreateDefaultFrontendModule())
    {
    }

    public DialectDslCompiler(DialectDslFrontendModule frontendModule)
    {
        frontendModule = frontendModule.ArgNotNull();

        var services = new ServiceCollection();
        services.AddDialectDslFrontendCompilerServices(frontendModule);
        _serviceProvider = services.BuildServiceProvider();

        var lexerFactory = _serviceProvider.GetRequiredService<Func<ILexer>>();
        var parserFactory = _serviceProvider.GetRequiredService<Func<IParser>>();
        var astTranslatorFactory = _serviceProvider.GetRequiredService<Func<IAstToBytecodeTranslator>>();
        var abstractMethodsTranslatorFactory = _serviceProvider.GetRequiredService<Func<IAbstractMethodsTranslator>>();
        var compiler = new DialectDefinitionSliceCompiler();

        _core = new BasicCoreImpl<DialectDefinitionSlice>(
            lexerFactory,
            parserFactory,
            astTranslatorFactory,
            abstractMethodsTranslatorFactory,
            () => compiler,
            () => new DialectDefinitionSliceExecutor(),
            [_serviceProvider.GetRequiredService<IFrontendCoreModule>()],
            [],
            []);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    public DialectDefinitionSlice Compile(string sourceText) => _core.GetExecutable(sourceText);

    private static DialectDslFrontendModule CreateDefaultFrontendModule() => DialectDslStandaloneComposition.CreateFrontendModule();
}