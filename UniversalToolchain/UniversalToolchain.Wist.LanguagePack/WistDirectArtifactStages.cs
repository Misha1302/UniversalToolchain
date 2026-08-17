using ArithmeticModule.Visitors;
using BasicCore.Binding;
using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Runtime;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistDirectArtifactKinds
{
    public static LanguageArtifactKind<WistSyntaxArtifact> Syntax { get; } = new(
        WistArtifactKinds.SyntaxTree,
        WistArtifactKinds.SyntaxTreeContract.ValueTypeIdentity!);

    public static LanguageArtifactKind<WistSemanticArtifact> Semantic { get; } = new(
        WistArtifactKinds.SemanticProgram,
        WistArtifactKinds.SemanticProgramContract.ValueTypeIdentity!);

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

    public CompilationInput CreateRuntimeInput(string source, IReadOnlyDictionary<string, object?> arguments)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(arguments);
        var parameters = new Dictionary<string, object>(arguments.Count, StringComparer.Ordinal);
        foreach (var pair in arguments)
            parameters.Add(pair.Key, WistRuntimeValueAdapterActivation.NormalizeInput(_plan, pair.Value)!);
        return _normalizer.NormalizeRuntimeInput(source, parameters);
    }

    public CompilationInput CreateDeclaredInput(string source, IReadOnlyList<LanguageBuildBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(bindings);
        var parameters = new OrderedDictionary<string, Type>(bindings.Count, StringComparer.Ordinal);
        foreach (var binding in bindings)
            parameters.Add(binding.Name, WistRuntimeValueAdapterActivation.NormalizeDeclaredType(_plan, binding.ValueType));
        return _normalizer.NormalizeDeclaredInput(source, parameters);
    }
}

internal sealed class WistSyntaxArtifact(CompilationInput input, AstNode root)
{
    public CompilationInput Input { get; } = input ?? throw new ArgumentNullException(nameof(input));
    public AstNode Root { get; } = root ?? throw new ArgumentNullException(nameof(root));
}

internal sealed class WistSemanticArtifact(CompilationInput input, WistSemanticProgram program)
{
    public CompilationInput Input { get; } = input ?? throw new ArgumentNullException(nameof(input));
    public WistSemanticProgram Program { get; } = program ?? throw new ArgumentNullException(nameof(program));
}

internal sealed class WistBytecodeArtifact(CompilationInput input, Bytecode bytecode)
{
    public CompilationInput Input { get; } = input ?? throw new ArgumentNullException(nameof(input));
    public Bytecode Bytecode { get; } = bytecode ?? throw new ArgumentNullException(nameof(bytecode));
}

internal sealed class WistOptimizerContractSnapshot : IModuleContractDescriptorProvider
{
    private readonly IReadOnlyList<IModuleContractFacet> _facets;

    private WistOptimizerContractSnapshot(
        LanguageContributionId contributionId,
        IReadOnlyList<ContractNamespaceOwner> namespaceOwners,
        IReadOnlyList<IModuleContractFacet> facets)
    {
        ContributionId = contributionId;
        NamespaceOwners = WistModuleContractSnapshotter.CaptureNamespaceOwners(namespaceOwners);
        _facets = WistModuleContractSnapshotter.CaptureFacets(facets);
    }

    public LanguageContributionId ContributionId { get; }
    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners { get; }
    public IReadOnlyList<IModuleContractFacet> GetFacets() => _facets;

    public static WistOptimizerContractSnapshot Capture(
        LanguageContributionId contributionId,
        IModuleContractDescriptorProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return new WistOptimizerContractSnapshot(
            contributionId,
            provider.NamespaceOwners,
            provider.GetFacets());
    }
}

internal sealed class WistAirArtifact(
    CompilationInput input,
    IAbstractIR air,
    SsaRouteReport? ssaReport = null,
    IReadOnlyList<WistOptimizerContractSnapshot>? appliedOptimizerContracts = null)
{
    public CompilationInput Input { get; } = input ?? throw new ArgumentNullException(nameof(input));
    public IAbstractIR Air { get; } = air ?? throw new ArgumentNullException(nameof(air));
    public SsaRouteReport? SsaReport { get; } = ssaReport;
    public IReadOnlyList<WistOptimizerContractSnapshot> AppliedOptimizerContracts { get; } =
        appliedOptimizerContracts?.ToArray() ?? [];
}

internal sealed class WistDirectArtifactStageFactory(
    Func<ILexer> lexerFactory,
    Func<IParser> parserFactory,
    Func<IAstToBytecodeTranslator> astTranslatorFactory,
    Func<IAbstractMethodsTranslator> abstractMethodsTranslatorFactory,
    IReadOnlyList<Func<IFrontendCoreModule>> syntaxModuleFactories,
    IReadOnlyList<Func<IFrontendCoreModule>> semanticModuleFactories,
    IReadOnlyList<Func<IFrontendCoreModule>> loweringModuleFactories,
    bool canonicalAddLowering,
    WistHostBindingAdapter hostBindingAdapter)
{
    private readonly Func<ILexer> _lexerFactory = lexerFactory ?? throw new ArgumentNullException(nameof(lexerFactory));
    private readonly Func<IParser> _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
    private readonly Func<IAstToBytecodeTranslator> _astTranslatorFactory = astTranslatorFactory ?? throw new ArgumentNullException(nameof(astTranslatorFactory));
    private readonly Func<IAbstractMethodsTranslator> _abstractMethodsTranslatorFactory = abstractMethodsTranslatorFactory ?? throw new ArgumentNullException(nameof(abstractMethodsTranslatorFactory));
    private readonly IReadOnlyList<Func<IFrontendCoreModule>> _syntaxModuleFactories = syntaxModuleFactories?.ToArray() ?? throw new ArgumentNullException(nameof(syntaxModuleFactories));
    private readonly IReadOnlyList<Func<IFrontendCoreModule>> _semanticModuleFactories = semanticModuleFactories?.ToArray() ?? throw new ArgumentNullException(nameof(semanticModuleFactories));
    private readonly IReadOnlyList<Func<IFrontendCoreModule>> _loweringModuleFactories = loweringModuleFactories?.ToArray() ?? throw new ArgumentNullException(nameof(loweringModuleFactories));
    private readonly bool _canonicalAddLowering = canonicalAddLowering;
    private readonly WistHostBindingAdapter _hostBindingAdapter = hostBindingAdapter ?? throw new ArgumentNullException(nameof(hostBindingAdapter));

    public WistDirectFrontendTransformer CreateFrontend() => new(_lexerFactory, _parserFactory, _syntaxModuleFactories, _hostBindingAdapter);
    public WistDirectSemanticTransformer CreateSemanticBinding() => new(_semanticModuleFactories);
    public WistDirectBytecodeTransformer CreateBytecodeLowering() => new(_astTranslatorFactory, _loweringModuleFactories, _canonicalAddLowering);
    public WistDirectAirTransformer CreateAirLowering() => new(_abstractMethodsTranslatorFactory);
}

internal sealed class WistDirectFrontendTransformer(
    Func<ILexer> lexerFactory,
    Func<IParser> parserFactory,
    IReadOnlyList<Func<IFrontendCoreModule>> moduleFactories,
    WistHostBindingAdapter hostBindingAdapter) :
    ILanguageArtifactTransformer<string, WistSyntaxArtifact>, ILanguageArtifactBuildTransformer
{
    private static readonly LanguageRuntimeComponentTraits Traits = LanguageRuntimeComponentTraits.Unknown;
    private readonly Func<ILexer> _lexerFactory = lexerFactory ?? throw new ArgumentNullException(nameof(lexerFactory));
    private readonly Func<IParser> _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
    private readonly IReadOnlyList<Func<IFrontendCoreModule>> _moduleFactories = moduleFactories?.ToArray() ?? throw new ArgumentNullException(nameof(moduleFactories));
    private readonly WistHostBindingAdapter _hostBindingAdapter = hostBindingAdapter ?? throw new ArgumentNullException(nameof(hostBindingAdapter));

    public LanguageContributionId ContributionId => WistContributionIds.Frontend;
    public LanguageArtifactKind<string> TypedSourceKind => StandardLanguageArtifactKinds.SourceText;
    public LanguageArtifactKind<WistSyntaxArtifact> TypedTargetKind => WistDirectArtifactKinds.Syntax;
    public LanguageRuntimeComponentTraits TypedTraits => Traits;

    public WistSyntaxArtifact Transform(string source, LanguageArtifactTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        return TransformCore(_hostBindingAdapter.CreateRuntimeInput(source, context.Request.Arguments));
    }

    public LanguageArtifact TransformForBuild(LanguageArtifact source, LanguageArtifactBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        var result = TransformCore(_hostBindingAdapter.CreateDeclaredInput(source.GetRequiredValue<string>(), context.Request.Bindings));
        return new LanguageArtifact<WistSyntaxArtifact>(WistDirectArtifactKinds.Syntax, result);
    }

    private WistSyntaxArtifact TransformCore(CompilationInput input)
    {
        var modules = WistPhaseModuleExecution.CreateModules(_moduleFactories);
        try
        {
            var root = WistPhaseModuleExecution.ParseSyntax(input, _lexerFactory(), _parserFactory(), modules);
            return new WistSyntaxArtifact(input, root);
        }
        finally
        {
            WistPhaseModuleExecution.DisposeModules(modules);
        }
    }
}

internal sealed class WistDirectSemanticTransformer(
    IReadOnlyList<Func<IFrontendCoreModule>> moduleFactories) : ILanguageArtifactTransformer<WistSyntaxArtifact, WistSemanticArtifact>
{
    private static readonly LanguageRuntimeComponentTraits Traits = LanguageRuntimeComponentTraits.DeterministicNoHostInterop;
    private readonly IReadOnlyList<Func<IFrontendCoreModule>> _moduleFactories = moduleFactories?.ToArray() ?? throw new ArgumentNullException(nameof(moduleFactories));

    public LanguageContributionId ContributionId => WistContributionIds.SemanticBinding;
    public LanguageArtifactKind<WistSyntaxArtifact> TypedSourceKind => WistDirectArtifactKinds.Syntax;
    public LanguageArtifactKind<WistSemanticArtifact> TypedTargetKind => WistDirectArtifactKinds.Semantic;
    public LanguageRuntimeComponentTraits TypedTraits => Traits;

    public WistSemanticArtifact Transform(WistSyntaxArtifact source, LanguageArtifactTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        var modules = WistPhaseModuleExecution.CreateModules(_moduleFactories);
        try
        {
            var boundRoot = WistPhaseModuleExecution.BindSemantic(source.Input, source.Root, modules);
            return new WistSemanticArtifact(source.Input, WistSemanticNormalizer.Normalize(boundRoot));
        }
        finally
        {
            WistPhaseModuleExecution.DisposeModules(modules);
        }
    }
}

internal sealed class WistDirectBytecodeTransformer(
    Func<IAstToBytecodeTranslator> translatorFactory,
    IReadOnlyList<Func<IFrontendCoreModule>> moduleFactories,
    bool canonicalAddLowering) : ILanguageArtifactTransformer<WistSemanticArtifact, WistBytecodeArtifact>
{
    private static readonly LanguageRuntimeComponentTraits Traits = LanguageRuntimeComponentTraits.Unknown;
    private readonly Func<IAstToBytecodeTranslator> _translatorFactory = translatorFactory ?? throw new ArgumentNullException(nameof(translatorFactory));
    private readonly IReadOnlyList<Func<IFrontendCoreModule>> _moduleFactories = moduleFactories?.ToArray() ?? throw new ArgumentNullException(nameof(moduleFactories));
    private readonly bool _canonicalAddLowering = canonicalAddLowering;

    public LanguageContributionId ContributionId => WistContributionIds.LoweringToBytecode;
    public LanguageArtifactKind<WistSemanticArtifact> TypedSourceKind => WistDirectArtifactKinds.Semantic;
    public LanguageArtifactKind<WistBytecodeArtifact> TypedTargetKind => WistDirectArtifactKinds.Bytecode;
    public LanguageRuntimeComponentTraits TypedTraits => Traits;

    public WistBytecodeArtifact Transform(WistSemanticArtifact source, LanguageArtifactTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        var modules = WistPhaseModuleExecution.CreateModules(_moduleFactories);
        try
        {
            var bytecode = WistPhaseModuleExecution.LowerToBytecode(
                WistSemanticNormalizer.ProjectForLegacyLowering(source.Program),
                _translatorFactory(),
                modules,
                _canonicalAddLowering);
            return new WistBytecodeArtifact(source.Input, bytecode);
        }
        finally
        {
            WistPhaseModuleExecution.DisposeModules(modules);
        }
    }
}

internal static class WistPhaseModuleExecution
{
    public static IReadOnlyList<IFrontendCoreModule> CreateModules(IReadOnlyList<Func<IFrontendCoreModule>> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);
        var modules = new List<IFrontendCoreModule>(factories.Count);
        try
        {
            for (var index = 0; index < factories.Count; index++)
            {
                var module = factories[index]() ?? throw new InvalidOperationException(
                    $"Wist phase module factory at index {index} returned null.");
                modules.Add(module);
            }
            return modules;
        }
        catch
        {
            DisposeModules(modules);
            throw;
        }
    }

    public static AstNode ParseSyntax(
        CompilationInput input,
        ILexer lexer,
        IParser parser,
        IReadOnlyList<IFrontendCoreModule> modules)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(lexer);
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(modules);

        var targetCode = modules.Aggregate(input.SourceText, static (current, module) => module.ProcessText(current));
        foreach (var module in modules)
            module.InitLexer(lexer);
        var lexemes = lexer.Lexemize(targetCode);

        var targetLexemes = modules.Aggregate(lexemes, static (current, module) => module.ProcessLexemes(current));
        foreach (var module in modules)
            module.InitParser(parser);
        var root = parser.Parse(targetLexemes);
        return modules.Aggregate(root, static (current, module) => module.ProcessAst(current));
    }

    public static AstNode BindSemantic(
        CompilationInput input,
        AstNode syntaxRoot,
        IReadOnlyList<IFrontendCoreModule> modules)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(syntaxRoot);
        ArgumentNullException.ThrowIfNull(modules);
        var bindingRules = modules.SelectMany(static module => module.GetAstBindingRules()).ToArray();
        return new Binder(input.ExternalBindings, bindingRules).Bind(syntaxRoot);
    }

    public static Bytecode LowerToBytecode(
        AstNode root,
        IAstToBytecodeTranslator translator,
        IReadOnlyList<IFrontendCoreModule> modules,
        bool canonicalAddLowering)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(translator);
        ArgumentNullException.ThrowIfNull(modules);

        translator.AddVisitors(new WistProgramStructureLoweringVisitor());
        if (canonicalAddLowering)
            translator.AddVisitors(new CanonicalAddSemanticAstVisitor());
        foreach (var module in modules)
            module.InitAstTranslator(translator, modules);
        var bytecode = translator.Translate(root);
        return modules.Aggregate(bytecode, static (current, module) => module.ProcessBytecode(current));
    }

    public static void DisposeModules(IEnumerable<IFrontendCoreModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        foreach (var module in modules.Reverse())
        {
            if (module is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private sealed class WistProgramStructureLoweringVisitor : IAstVisitor
    {
        public void TryVisit(BytecodeVisitorData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Program"))
                return;
            foreach (var child in data.Node.Children)
                data.AstToBytecodeTranslator.Translate(child);
        }
    }
}

internal sealed class WistDirectAirTransformer(Func<IAbstractMethodsTranslator> translatorFactory) : ILanguageArtifactTransformer<WistBytecodeArtifact, WistAirArtifact>
{
    private static readonly LanguageRuntimeComponentTraits Traits = LanguageRuntimeComponentTraits.Unknown;
    private readonly Func<IAbstractMethodsTranslator> _translatorFactory = translatorFactory ?? throw new ArgumentNullException(nameof(translatorFactory));
    public LanguageContributionId ContributionId => WistContributionIds.LoweringToAir;
    public LanguageArtifactKind<WistBytecodeArtifact> TypedSourceKind => WistDirectArtifactKinds.Bytecode;
    public LanguageArtifactKind<WistAirArtifact> TypedTargetKind => WistDirectArtifactKinds.Air;
    public LanguageRuntimeComponentTraits TypedTraits => Traits;

    public WistAirArtifact Transform(WistBytecodeArtifact source, LanguageArtifactTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        return new WistAirArtifact(source.Input, CanonicalArtifactStages.LowerToAir(source.Bytecode, _translatorFactory()));
    }
}
