using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Frontend.Composition;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslCompiler : IDisposable
{
    private readonly Func<ILexer> _lexerFactory;
    private readonly Func<IParser> _parserFactory;
    private readonly Func<IAstToBytecodeTranslator> _astTranslatorFactory;
    private readonly Func<IAbstractMethodsTranslator> _abstractMethodsTranslatorFactory;
    private readonly IFrontendCoreModule _frontendModule;
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
        _lexerFactory = _serviceProvider.GetRequiredService<Func<ILexer>>();
        _parserFactory = _serviceProvider.GetRequiredService<Func<IParser>>();
        _astTranslatorFactory = _serviceProvider.GetRequiredService<Func<IAstToBytecodeTranslator>>();
        _abstractMethodsTranslatorFactory = _serviceProvider.GetRequiredService<Func<IAbstractMethodsTranslator>>();
        _frontendModule = _serviceProvider.GetRequiredService<IFrontendCoreModule>();
    }

    public void Dispose() => _serviceProvider.Dispose();

    public DialectDefinitionSlice Compile(string sourceText)
    {
        sourceText = sourceText.ArgNotNull();
        var input = new CompilationInputNormalizer().NormalizeRuntimeInput(sourceText);
        IFrontendCoreModule[] modules = [_frontendModule];
        var root = CanonicalArtifactStages.ParseAndBind(input, _lexerFactory(), _parserFactory(), modules);
        var bytecode = CanonicalArtifactStages.LowerToBytecode(root, _astTranslatorFactory(), modules);
        var air = CanonicalArtifactStages.LowerToAir(bytecode, _abstractMethodsTranslatorFactory());
        return new DialectDefinitionSliceCompiler().Compile(air, input);
    }

    private static DialectDslFrontendModule CreateDefaultFrontendModule() => DialectDslStandaloneComposition.CreateFrontendModule();
}
