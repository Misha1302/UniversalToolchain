using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistDirectArtifactKinds
{
    public static LanguageArtifactKind<WistSyntaxArtifact> Syntax { get; } = new(
        WistArtifactKinds.SyntaxTree,
        WistArtifactKinds.SyntaxTreeContract.ValueTypeIdentity!);

    public static LanguageArtifactKind<WistBytecodeArtifact> Bytecode { get; } = new(
        WistArtifactKinds.Bytecode,
        WistArtifactKinds.BytecodeContract.ValueTypeIdentity!);

    public static LanguageArtifactKind<WistAirArtifact> Air { get; } = new(
        LanguageArtifacts.Air,
        WistArtifactKinds.AirContract.ValueTypeIdentity!);
}

internal sealed class WistHostBindingAdapter(LanguagePlan plan)
{
    private readonly LanguagePlan _plan = plan ?? throw new ArgumentNullException(nameof(plan));
    private readonly CompilationInputNormalizer _normalizer = new();

    public CompilationInput CreateRuntimeInput(
        string source,
        IReadOnlyDictionary<string, object?> arguments)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(arguments);

        var parameters = new Dictionary<string, object>(arguments.Count, StringComparer.Ordinal);
        foreach (var pair in arguments)
            parameters.Add(pair.Key, WistRuntimeValueAdapterActivation.NormalizeInput(_plan, pair.Value)!);
        return _normalizer.NormalizeRuntimeInput(source, parameters);
    }

    public CompilationInput CreateDeclaredInput(
        string source,
        IReadOnlyList<LanguageBuildBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(bindings);

        var parameters = new OrderedDictionary<string, Type>(bindings.Count, StringComparer.Ordinal);
        foreach (var binding in bindings)
        {
            parameters.Add(
                binding.Name,
                WistRuntimeValueAdapterActivation.NormalizeDeclaredType(_plan, binding.ValueType));
        }
        return _normalizer.NormalizeDeclaredInput(source, parameters);
    }
}

internal sealed class WistSyntaxArtifact(
    CompilationInput input,
    AstNode root,
    IReadOnlyList<IFrontendCoreModule> modules,
    IWistCompilationObservation observation)
{
    public CompilationInput Input { get; } = input ?? throw new ArgumentNullException(nameof(input));
    public AstNode Root { get; } = root ?? throw new ArgumentNullException(nameof(root));
    public IReadOnlyList<IFrontendCoreModule> Modules { get; } = modules?.ToArray()
        ?? throw new ArgumentNullException(nameof(modules));
    public IWistCompilationObservation Observation { get; } = observation
        ?? throw new ArgumentNullException(nameof(observation));
}

internal sealed class WistBytecodeArtifact(
    CompilationInput input,
    Bytecode bytecode,
    IReadOnlyList<IFrontendCoreModule> modules,
    IWistCompilationObservation observation)
{
    public CompilationInput Input { get; } = input ?? throw new ArgumentNullException(nameof(input));
    public Bytecode Bytecode { get; } = bytecode ?? throw new ArgumentNullException(nameof(bytecode));
    public IReadOnlyList<IFrontendCoreModule> Modules { get; } = modules?.ToArray()
        ?? throw new ArgumentNullException(nameof(modules));
    public IWistCompilationObservation Observation { get; } = observation
        ?? throw new ArgumentNullException(nameof(observation));
}

internal sealed class WistAirArtifact(
    CompilationInput input,
    IAbstractIR air,
    IReadOnlyList<IFrontendCoreModule> modules,
    IReadOnlyList<IAirOptimizer> optimizers,
    IWistCompilationObservation observation,
    SsaRouteReport? ssaReport = null)
{
    public CompilationInput Input { get; } = input ?? throw new ArgumentNullException(nameof(input));
    public IAbstractIR Air { get; } = air ?? throw new ArgumentNullException(nameof(air));
    public IReadOnlyList<IFrontendCoreModule> Modules { get; } = modules?.ToArray()
        ?? throw new ArgumentNullException(nameof(modules));
    public IReadOnlyList<IAirOptimizer> Optimizers { get; } = optimizers?.ToArray()
        ?? throw new ArgumentNullException(nameof(optimizers));
    public IWistCompilationObservation Observation { get; } = observation
        ?? throw new ArgumentNullException(nameof(observation));
    public SsaRouteReport? SsaReport { get; } = ssaReport;
}

internal sealed class WistDirectArtifactStageFactory(
    Func<ILexer> lexerFactory,
    Func<IParser> parserFactory,
    Func<IAstToBytecodeTranslator> astTranslatorFactory,
    Func<IAbstractMethodsTranslator> abstractMethodsTranslatorFactory,
    IReadOnlyList<Func<IFrontendCoreModule>> moduleFactories,
    WistHostBindingAdapter hostBindingAdapter,
    IWistCompilationObservationFactory observationFactory)
{
    private readonly Func<ILexer> _lexerFactory = lexerFactory ?? throw new ArgumentNullException(nameof(lexerFactory));
    private readonly Func<IParser> _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
    private readonly Func<IAstToBytecodeTranslator> _astTranslatorFactory = astTranslatorFactory ?? throw new ArgumentNullException(nameof(astTranslatorFactory));
    private readonly Func<IAbstractMethodsTranslator> _abstractMethodsTranslatorFactory = abstractMethodsTranslatorFactory ?? throw new ArgumentNullException(nameof(abstractMethodsTranslatorFactory));
    private readonly IReadOnlyList<Func<IFrontendCoreModule>> _moduleFactories = moduleFactories?.ToArray()
        ?? throw new ArgumentNullException(nameof(moduleFactories));
    private readonly WistHostBindingAdapter _hostBindingAdapter = hostBindingAdapter ?? throw new ArgumentNullException(nameof(hostBindingAdapter));
    private readonly IWistCompilationObservationFactory _observationFactory = observationFactory
        ?? throw new ArgumentNullException(nameof(observationFactory));

    public WistDirectFrontendTransformer CreateFrontend() => new(
        _lexerFactory,
        _parserFactory,
        _moduleFactories,
        _hostBindingAdapter,
        _observationFactory);

    public WistDirectBytecodeTransformer CreateBytecodeLowering() => new(_astTranslatorFactory);

    public WistDirectAirTransformer CreateAirLowering() => new(_abstractMethodsTranslatorFactory);
}

internal sealed class WistDirectFrontendTransformer(
    Func<ILexer> lexerFactory,
    Func<IParser> parserFactory,
    IReadOnlyList<Func<IFrontendCoreModule>> moduleFactories,
    WistHostBindingAdapter hostBindingAdapter,
    IWistCompilationObservationFactory observationFactory) :
    ILanguageArtifactTransformer<string, WistSyntaxArtifact>,
    ILanguageArtifactBuildTransformer
{
    private static readonly LanguageRuntimeComponentTraits Traits = LanguageRuntimeComponentTraits.Unknown;
    private readonly Func<ILexer> _lexerFactory = lexerFactory ?? throw new ArgumentNullException(nameof(lexerFactory));
    private readonly Func<IParser> _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
    private readonly IReadOnlyList<Func<IFrontendCoreModule>> _moduleFactories = moduleFactories?.ToArray()
        ?? throw new ArgumentNullException(nameof(moduleFactories));
    private readonly WistHostBindingAdapter _hostBindingAdapter = hostBindingAdapter ?? throw new ArgumentNullException(nameof(hostBindingAdapter));
    private readonly IWistCompilationObservationFactory _observationFactory = observationFactory
        ?? throw new ArgumentNullException(nameof(observationFactory));

    public LanguageContributionId ContributionId => WistContributionIds.Frontend;
    public LanguageArtifactKind<string> TypedSourceKind => StandardLanguageArtifactKinds.SourceText;
    public LanguageArtifactKind<WistSyntaxArtifact> TypedTargetKind => WistDirectArtifactKinds.Syntax;
    public LanguageRuntimeComponentTraits TypedTraits => Traits;

    public WistSyntaxArtifact Transform(string source, LanguageArtifactTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        return TransformCore(
            _hostBindingAdapter.CreateRuntimeInput(source, context.Request.Arguments),
            context.Request.Backend);
    }

    public LanguageArtifact TransformForBuild(LanguageArtifact source, LanguageArtifactBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        var sourceText = source.GetRequiredValue<string>();
        var result = TransformCore(
            _hostBindingAdapter.CreateDeclaredInput(sourceText, context.Request.Bindings),
            context.Request.Backend);
        return new LanguageArtifact<WistSyntaxArtifact>(WistDirectArtifactKinds.Syntax, result);
    }

    private WistSyntaxArtifact TransformCore(CompilationInput input, BackendId backend)
    {
        var modules = new IFrontendCoreModule[_moduleFactories.Count];
        for (var i = 0; i < _moduleFactories.Count; i++)
        {
            modules[i] = _moduleFactories[i]()
                ?? throw new InvalidOperationException($"Wist frontend module factory at index {i} returned null.");
        }

        var root = CanonicalArtifactStages.ParseAndBind(input, _lexerFactory(), _parserFactory(), modules);
        return new WistSyntaxArtifact(input, root, modules, _observationFactory.Create(backend));
    }
}

internal sealed class WistDirectBytecodeTransformer(
    Func<IAstToBytecodeTranslator> translatorFactory) : ILanguageArtifactTransformer<WistSyntaxArtifact, WistBytecodeArtifact>
{
    private static readonly LanguageRuntimeComponentTraits Traits = LanguageRuntimeComponentTraits.Unknown;
    private readonly Func<IAstToBytecodeTranslator> _translatorFactory = translatorFactory
        ?? throw new ArgumentNullException(nameof(translatorFactory));

    public LanguageContributionId ContributionId => WistContributionIds.LoweringToBytecode;
    public LanguageArtifactKind<WistSyntaxArtifact> TypedSourceKind => WistDirectArtifactKinds.Syntax;
    public LanguageArtifactKind<WistBytecodeArtifact> TypedTargetKind => WistDirectArtifactKinds.Bytecode;
    public LanguageRuntimeComponentTraits TypedTraits => Traits;

    public WistBytecodeArtifact Transform(WistSyntaxArtifact source, LanguageArtifactTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);

        var bytecode = CanonicalArtifactStages.LowerToBytecode(source.Root, _translatorFactory(), source.Modules);
        source.Observation.AfterBytecode(source.Input, source.Modules, bytecode);
        return new WistBytecodeArtifact(source.Input, bytecode, source.Modules, source.Observation);
    }
}

internal sealed class WistDirectAirTransformer(
    Func<IAbstractMethodsTranslator> translatorFactory) : ILanguageArtifactTransformer<WistBytecodeArtifact, WistAirArtifact>
{
    private static readonly LanguageRuntimeComponentTraits Traits = LanguageRuntimeComponentTraits.Unknown;
    private readonly Func<IAbstractMethodsTranslator> _translatorFactory = translatorFactory
        ?? throw new ArgumentNullException(nameof(translatorFactory));

    public LanguageContributionId ContributionId => WistContributionIds.LoweringToAir;
    public LanguageArtifactKind<WistBytecodeArtifact> TypedSourceKind => WistDirectArtifactKinds.Bytecode;
    public LanguageArtifactKind<WistAirArtifact> TypedTargetKind => WistDirectArtifactKinds.Air;
    public LanguageRuntimeComponentTraits TypedTraits => Traits;

    public WistAirArtifact Transform(WistBytecodeArtifact source, LanguageArtifactTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);

        var air = CanonicalArtifactStages.LowerToAir(source.Bytecode, _translatorFactory());
        source.Observation.AfterAir(source.Input, source.Modules, [], air);
        return new WistAirArtifact(source.Input, air, source.Modules, [], source.Observation);
    }
}
