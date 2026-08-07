using AbstractIrConverters;
using BasicCilCompiler.Execution;
using BasicCodeTranslator;
using BasicCore.Capabilities;
using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.Execution;
using BasicCore.Registration;
using BasicInterpreter;
using BasicLexer.Core;
using BasicParser.Core;
using BytecodeDynamicMethodsCompiler.Compilers;
using ConditionsModule.Optimizers;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using NativeMathModule;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Runtime;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistDirectBackendArtifactKinds
{
    public static LanguageArtifactKind<WistInterpreterArtifact> Interpreter { get; } = new(
        WistArtifactKinds.InterpreterArtifact,
        WistArtifactKinds.InterpreterArtifactContract.ValueTypeIdentity!);

    public static LanguageArtifactKind<WistCilArtifact> Cil { get; } = new(
        WistArtifactKinds.CilArtifact,
        WistArtifactKinds.CilArtifactContract.ValueTypeIdentity!);
}

internal sealed class WistInterpreterArtifact(CompilationInput input, IAbstractIR air)
{
    public CompilationInput Input { get; } = input ?? throw new ArgumentNullException(nameof(input));
    public IAbstractIR Air { get; } = air ?? throw new ArgumentNullException(nameof(air));
}

internal sealed class WistCilArtifact(CompilationInput input, CilCompilationOutput compilation)
{
    public CompilationInput Input { get; } = input ?? throw new ArgumentNullException(nameof(input));
    public CilCompilationOutput Compilation { get; } = compilation ?? throw new ArgumentNullException(nameof(compilation));
}

internal static class WistDirectRuntimeComponents
{
    private static readonly BackendId InterpreterBackend = new("interpreter");
    private static readonly BackendId CilBackend = new("cil");
    private static readonly LanguageRuntimeComponentTraits DirectTraits = new(
        LanguageComponentDeterminism.Unknown,
        LanguageComponentHostInterop.None);

    public static LanguageRouteComponentCatalog CreateCatalog(WistLanguageFeaturePackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        var registry = new LanguageRouteComponentRegistry();
        registry.AddTransformer(CreateFrontendRegistration(package));
        registry.AddTransformer(CreateBytecodeRegistration());
        registry.AddTransformer(CreateAirRegistration());
        foreach (var optimizer in WistRuntimeComponentCatalog.Optimizers)
            registry.AddTransformer(CreateOptimizerRegistration(optimizer));
        registry.AddTransformer(CreateInterpreterBackendRegistration());
        registry.AddExecutor(CreateInterpreterExecutorRegistration());
        registry.AddTransformer(CreateCilBackendRegistration());
        registry.AddExecutor(CreateCilExecutorRegistration());
        return registry.CreateCatalog();
    }

    private static LanguageTransformerRegistration CreateFrontendRegistration(WistLanguageFeaturePackage package) =>
        LanguageTransformerRegistration.Create<string, WistSyntaxArtifact>(
            WistContributionIds.Frontend,
            StandardLanguageArtifactKinds.SourceText,
            WistDirectArtifactKinds.Syntax,
            DirectTraits,
            context =>
            {
                var exposedAssemblies = context.Options.AllowedAssemblies
                    .Append(typeof(BasicStdLib.Main).Assembly)
                    .Distinct()
                    .ToArray();
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddSingleton<ITypeCatalog>(TypeCatalogFactory.Create(exposedAssemblies));
                serviceCollection.AddSingleton<IMethodResolver, DeterministicMethodResolver>();
                var services = serviceCollection.BuildServiceProvider();
                var moduleFactories = WistFrontendModuleActivation.CreateOrderedFactories(
                    context.Plan,
                    [WistFrontendModuleActivation.CreateBuiltInSource(package)],
                    services);
                var stageFactory = new WistDirectArtifactStageFactory(
                    static () => new BasicLexerImpl(),
                    static () => new BasicParserImpl(),
                    static () => new BasicAstToBytecodeTranslatorImpl(),
                    CreateMethodsTranslator,
                    moduleFactories);
                return new OwnedFrontendTransformer(stageFactory.CreateFrontend(), services, DirectTraits);
            });

    private static LanguageTransformerRegistration CreateBytecodeRegistration() =>
        LanguageTransformerRegistration.Create<WistSyntaxArtifact, WistBytecodeArtifact>(
            WistContributionIds.LoweringToBytecode,
            WistDirectArtifactKinds.Syntax,
            WistDirectArtifactKinds.Bytecode,
            DirectTraits,
            _ => new TraitsOverrideTransformer<WistSyntaxArtifact, WistBytecodeArtifact>(
                new WistDirectBytecodeTransformer(static () => new BasicAstToBytecodeTranslatorImpl()),
                DirectTraits));

    private static LanguageTransformerRegistration CreateAirRegistration() =>
        LanguageTransformerRegistration.Create<WistBytecodeArtifact, WistAirArtifact>(
            WistContributionIds.LoweringToAir,
            WistDirectArtifactKinds.Bytecode,
            WistDirectArtifactKinds.Air,
            DirectTraits,
            _ => new TraitsOverrideTransformer<WistBytecodeArtifact, WistAirArtifact>(
                new WistDirectAirTransformer(CreateMethodsTranslator),
                DirectTraits));

    private static LanguageTransformerRegistration CreateOptimizerRegistration(WistRuntimeComponentDescriptor component) =>
        LanguageTransformerRegistration.Create<WistAirArtifact, WistAirArtifact>(
            component.ContributionId,
            WistDirectArtifactKinds.Air,
            WistDirectArtifactKinds.Air,
            DirectTraits,
            context => new WistDirectOptimizerTransformer(
                component.ContributionId,
                () => CreateOptimizer(component.ContributionId, context.Plan),
                DirectTraits));

    private static LanguageTransformerRegistration CreateInterpreterBackendRegistration() =>
        LanguageTransformerRegistration.Create<WistAirArtifact, WistInterpreterArtifact>(
            WistContributionIds.InterpreterBackend,
            WistDirectArtifactKinds.Air,
            WistDirectBackendArtifactKinds.Interpreter,
            DirectTraits,
            _ => new DelegateLanguageArtifactTransformer<WistAirArtifact, WistInterpreterArtifact>(
                WistContributionIds.InterpreterBackend,
                WistDirectArtifactKinds.Air,
                WistDirectBackendArtifactKinds.Interpreter,
                static (source, _) => new WistInterpreterArtifact(source.Input, source.Air),
                DirectTraits));

    private static LanguageExecutorRegistration CreateInterpreterExecutorRegistration() =>
        LanguageExecutorRegistration.Create<WistInterpreterArtifact, object?>(
            WistContributionIds.InterpreterBackend,
            InterpreterBackend,
            WistDirectBackendArtifactKinds.Interpreter,
            DirectTraits,
            _ => new DelegateLanguageArtifactExecutor<WistInterpreterArtifact, object?>(
                WistContributionIds.InterpreterBackend,
                InterpreterBackend,
                WistDirectBackendArtifactKinds.Interpreter,
                static (artifact, context) =>
                {
                    var environment = new ExecutionEnvironment(artifact.Input.ExternalBindings);
                    var value = new InterpreterImpl().Execute(artifact.Air, environment);
                    return WistRuntimeValueAdapterActivation.Normalize(context.Plan, value);
                },
                DirectTraits));

    private static LanguageTransformerRegistration CreateCilBackendRegistration() =>
        LanguageTransformerRegistration.Create<WistAirArtifact, WistCilArtifact>(
            WistContributionIds.CilBackend,
            WistDirectArtifactKinds.Air,
            WistDirectBackendArtifactKinds.Cil,
            DirectTraits,
            _ => new DelegateLanguageArtifactTransformer<WistAirArtifact, WistCilArtifact>(
                WistContributionIds.CilBackend,
                WistDirectArtifactKinds.Air,
                WistDirectBackendArtifactKinds.Cil,
                static (source, _) => new WistCilArtifact(
                    source.Input,
                    new AbstractMethodsCompilerImpl().Compile(source.Air, source.Input)),
                DirectTraits));

    private static LanguageExecutorRegistration CreateCilExecutorRegistration() =>
        LanguageExecutorRegistration.Create<WistCilArtifact, object?>(
            WistContributionIds.CilBackend,
            CilBackend,
            WistDirectBackendArtifactKinds.Cil,
            DirectTraits,
            _ => new DelegateLanguageArtifactExecutor<WistCilArtifact, object?>(
                WistContributionIds.CilBackend,
                CilBackend,
                WistDirectBackendArtifactKinds.Cil,
                static (artifact, context) =>
                {
                    var environment = new ExecutionEnvironment(artifact.Input.ExternalBindings);
                    var value = new DynamicMethodExecutor().Execute(artifact.Compilation, environment);
                    return WistRuntimeValueAdapterActivation.Normalize(context.Plan, value);
                },
                DirectTraits));

    private static IAirOptimizer CreateOptimizer(LanguageContributionId contributionId, UniversalToolchain.LanguageSdk.LanguagePlan plan)
    {
        if (contributionId == WistContributionIds.ArithmeticOptimizer)
            return new ArithmeticOptimizerModule();
        if (contributionId == WistContributionIds.BooleanOptimizer)
            return new BooleanOptimizerModule();
        if (contributionId == WistContributionIds.ComparisonIntrinsicOptimizer)
            return new ComparisonIntrinsicOptimizerModule();
        if (contributionId == WistContributionIds.EGraphOptimizer)
            return new EGraphOptimizerModule();
        if (contributionId == WistContributionIds.NativeCilOptimizer)
            return new NativeCilOptimizerModule();
        if (contributionId == WistContributionIds.NativeTypesOptimizer)
            return new NativeTypesOptimizerModule();
        if (contributionId == WistContributionIds.SsaOptimizer)
        {
            return new SsaOptimizerModule(
                WistSsaPlanPolicy.CreateRuntimeOptions(plan),
                NullSsaRouteReportSink.Instance,
                []);
        }

        throw new InvalidOperationException(
            $"Wist optimizer contribution '{contributionId.Value}' has no direct runtime factory.");
    }

    private static IAbstractMethodsTranslator CreateMethodsTranslator()
    {
        using var services = new ServiceCollection()
            .AddCoreIntrinsicServices()
            .BuildServiceProvider();
        return new BytecodeToAbstractIrConverterImpl(
            services.GetRequiredService<IInstructionIntrinsicReader>(),
            services.GetRequiredService<IIntrinsicTypeStackProcessor>());
    }

    private static IOptimizerIntrinsicCapabilityContext CreateCapabilityContext(BackendId backend)
    {
        if (backend == CilBackend)
        {
            return new OptimizerIntrinsicCapabilityContext(
                new CompilerIntrinsicCapabilitySetFactory().Create(new AbstractMethodsCompilerImpl()));
        }
        if (backend == InterpreterBackend)
        {
            return new OptimizerIntrinsicCapabilityContext(
                new CompilerIntrinsicCapabilitySetFactory().Create(new AbstractIrToAbstractIrStub()));
        }
        throw new InvalidOperationException($"Backend '{backend.Value}' is not supported by direct Wist optimization.");
    }

    private sealed class WistDirectOptimizerTransformer(
        LanguageContributionId contributionId,
        Func<IAirOptimizer> optimizerFactory,
        LanguageRuntimeComponentTraits traits) : ILanguageArtifactTransformer<WistAirArtifact, WistAirArtifact>
    {
        public LanguageContributionId ContributionId { get; } = contributionId;
        public LanguageArtifactKind<WistAirArtifact> TypedSourceKind => WistDirectArtifactKinds.Air;
        public LanguageArtifactKind<WistAirArtifact> TypedTargetKind => WistDirectArtifactKinds.Air;
        public LanguageRuntimeComponentTraits TypedTraits { get; } = traits;

        public WistAirArtifact Transform(WistAirArtifact source, LanguageArtifactTransformationContext context)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(context);

            var optimizer = optimizerFactory() ?? throw new InvalidOperationException(
                $"Wist optimizer factory '{ContributionId.Value}' returned null.");
            optimizer.InitMethodsTranslator(CreateMethodsTranslator());
            optimizer.InitIntrinsicCapabilityContext(CreateCapabilityContext(context.Request.Backend));
            var result = optimizer.Optimize(source.Air)
                ?? throw new InvalidOperationException($"Wist optimizer '{ContributionId.Value}' returned null AIR.");
            return new WistAirArtifact(source.Input, result);
        }
    }

    private sealed class TraitsOverrideTransformer<TSource, TTarget>(
        ILanguageArtifactTransformer<TSource, TTarget> inner,
        LanguageRuntimeComponentTraits traits) : ILanguageArtifactTransformer<TSource, TTarget>
    {
        public LanguageContributionId ContributionId => inner.ContributionId;
        public LanguageArtifactKind<TSource> TypedSourceKind => inner.TypedSourceKind;
        public LanguageArtifactKind<TTarget> TypedTargetKind => inner.TypedTargetKind;
        public LanguageRuntimeComponentTraits TypedTraits { get; } = traits;
        public TTarget Transform(TSource source, LanguageArtifactTransformationContext context) => inner.Transform(source, context);
    }

    private sealed class OwnedFrontendTransformer(
        ILanguageArtifactTransformer<string, WistSyntaxArtifact> inner,
        ServiceProvider owner,
        LanguageRuntimeComponentTraits traits) : ILanguageArtifactTransformer<string, WistSyntaxArtifact>, IDisposable
    {
        public LanguageContributionId ContributionId => inner.ContributionId;
        public LanguageArtifactKind<string> TypedSourceKind => inner.TypedSourceKind;
        public LanguageArtifactKind<WistSyntaxArtifact> TypedTargetKind => inner.TypedTargetKind;
        public LanguageRuntimeComponentTraits TypedTraits { get; } = traits;
        public WistSyntaxArtifact Transform(string source, LanguageArtifactTransformationContext context) => inner.Transform(source, context);
        public void Dispose() => owner.Dispose();
    }
}
