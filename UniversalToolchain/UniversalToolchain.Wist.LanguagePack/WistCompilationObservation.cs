using AbstractIrConverters;
using BasicCilCompiler.Contracts;
using BasicCore.Builtins;
using BasicCore.Compilation;
using BasicCore.Core;
using BasicCore.Contracts;
using BasicCore.TranslatorWrapper;
using BasicInterpreter.Contracts;
using BytecodeDynamicMethodsCompiler.Compilers;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.Wist.LanguagePack;

internal enum WistCompilationVerificationPolicy
{
    Disabled,
    P0Structural,
    P1Invalidation,
    P2Selective,
    P3Always
}

internal interface IWistCompilationInfrastructureModule
{
}

internal interface IWistCompilationObservationFactory
{
    IWistCompilationObservation Create(BackendId backend);
}

internal interface IWistCompilationObservation
{
    void AfterBytecode(
        CompilationInput input,
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        Bytecode bytecode);

    void AfterAir(
        CompilationInput input,
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IAirOptimizer> optimizers,
        IAbstractIR air);

    void AfterOptimizedAir(
        CompilationInput input,
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IAirOptimizer> optimizers,
        IAbstractIR air);
}

internal sealed class WistCompilationObservationFactory(WistCompilationVerificationPolicy policy)
    : IWistCompilationObservationFactory
{
    public IWistCompilationObservation Create(BackendId backend)
    {
        if (policy == WistCompilationVerificationPolicy.Disabled)
            return NullWistCompilationObservation.Instance;

        var (supportedIntrinsics, backendComponents) = CreateBackendContext(backend);
        var pipelineOptions = ModuleContractPipelineProfiles.StrictEnforced with
        {
            VerificationPolicy = MapPolicy(policy)
        };
        var sink = new InMemoryModuleContractDiagnosticSink();
        var observer = new ModuleContractPipelineObserver(
            pipelineOptions,
            new SelectedModuleContractTableProvider(
                pipelineOptions.EnforcementPolicy,
                new ModuleContractSelectionBuilder()),
            new BytecodeObservedEmissionReader(),
            new BytecodeVerifier(),
            CreateAirVerifier(),
            new BackendCapabilitySelectionFactory(pipelineOptions.BackendPolicy),
            new ModuleContractDiagnosticPolicy(sink),
            new PipelineEffectVerifier(),
            CompilerFactVerifierRegistry.Core,
            new CoreCompilerStageFactSeedProvider());
        return new ModuleContractWistCompilationObservation(
            observer,
            supportedIntrinsics,
            backendComponents);
    }

    private static (IReadOnlyList<string> SupportedIntrinsics, IReadOnlyList<IBackendPipelineComponent> BackendComponents)
        CreateBackendContext(BackendId backend)
    {
        if (backend == new BackendId("cil"))
        {
            var supported = AbstractMethodsCompilerImpl.SupportedIntrinsicIds.ToArray();
            return (
                supported,
                [
                    new ModuleContractBackendPipelineComponent(
                        CilBackendContractDescriptorProvider.Module.Value,
                        [new CilBackendContractDescriptorProvider(supported)])
                ]);
        }

        if (backend == new BackendId("interpreter"))
        {
            var supported = AbstractIrToAbstractIrStub.SupportedIntrinsicIds.ToArray();
            return (
                supported,
                [
                    new ModuleContractBackendPipelineComponent(
                        InterpreterBackendContractDescriptorProvider.Module.Value,
                        [new InterpreterBackendContractDescriptorProvider(supported)])
                ]);
        }

        throw new InvalidOperationException(
            $"Wist compilation observation does not recognize backend '{backend.Value}'.");
    }

    private static AirVerifier CreateAirVerifier()
    {
        var catalog = new IntrinsicCatalogBuilder().Build(
        [
            new CoreIntrinsicDescriptorProvider(new MethodCallTypeSemanticsResolver()),
            new ArithmeticIntrinsicDescriptorProvider(),
            new ComparisonIntrinsicDescriptorProvider(),
            new BooleanIntrinsicDescriptorProvider(),
            new StorageIntrinsicDescriptorProvider()
        ]);
        return new AirVerifier(
            new InstructionIntrinsicReader(),
            new IntrinsicTypeStackProcessor(catalog, new IntrinsicTypeResolutionContext()));
    }

    private static ModuleContractVerificationPolicy MapPolicy(WistCompilationVerificationPolicy value) => value switch
    {
        WistCompilationVerificationPolicy.P0Structural => ModuleContractVerificationPolicy.P0Structural,
        WistCompilationVerificationPolicy.P1Invalidation => ModuleContractVerificationPolicy.P1Invalidation,
        WistCompilationVerificationPolicy.P2Selective => ModuleContractVerificationPolicy.P2Selective,
        WistCompilationVerificationPolicy.P3Always => ModuleContractVerificationPolicy.P3Always,
        WistCompilationVerificationPolicy.Disabled => throw new InvalidOperationException(
            "Disabled Wist verification does not create a module-contract observer."),
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown Wist compilation verification policy.")
    };

    private sealed class ModuleContractWistCompilationObservation(
        ICompilationPipelineObserver observer,
        IReadOnlyList<string> supportedIntrinsics,
        IReadOnlyList<IBackendPipelineComponent> backendComponents) : IWistCompilationObservation
    {
        private readonly ICompilationPipelineObserver _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        private readonly IReadOnlyList<string> _supportedIntrinsics = supportedIntrinsics?.ToArray()
            ?? throw new ArgumentNullException(nameof(supportedIntrinsics));
        private readonly IReadOnlyList<IBackendPipelineComponent> _backendComponents = backendComponents?.ToArray()
            ?? throw new ArgumentNullException(nameof(backendComponents));

        public void AfterBytecode(
            CompilationInput input,
            IReadOnlyList<IFrontendCoreModule> frontendModules,
            Bytecode bytecode) =>
            _observer.AfterBytecode(new CompilationPipelineBytecodeContext(
                input,
                SemanticModules(frontendModules),
                bytecode,
                _backendComponents));

        public void AfterAir(
            CompilationInput input,
            IReadOnlyList<IFrontendCoreModule> frontendModules,
            IReadOnlyList<IAirOptimizer> optimizers,
            IAbstractIR air) =>
            _observer.AfterAir(new CompilationPipelineAirContext(
                input,
                SemanticModules(frontendModules),
                optimizers,
                air,
                _supportedIntrinsics,
                _backendComponents));

        public void AfterOptimizedAir(
            CompilationInput input,
            IReadOnlyList<IFrontendCoreModule> frontendModules,
            IReadOnlyList<IAirOptimizer> optimizers,
            IAbstractIR air) =>
            _observer.AfterOptimizedAir(new CompilationPipelineAirContext(
                input,
                SemanticModules(frontendModules),
                optimizers,
                air,
                _supportedIntrinsics,
                _backendComponents));

        private static IReadOnlyList<IFrontendCoreModule> SemanticModules(
            IReadOnlyList<IFrontendCoreModule> frontendModules) =>
            frontendModules
                .Where(static module => module is not IWistCompilationInfrastructureModule)
                .ToArray();
    }

    private sealed class NullWistCompilationObservation : IWistCompilationObservation
    {
        public static NullWistCompilationObservation Instance { get; } = new();

        public void AfterBytecode(
            CompilationInput input,
            IReadOnlyList<IFrontendCoreModule> frontendModules,
            Bytecode bytecode)
        {
        }

        public void AfterAir(
            CompilationInput input,
            IReadOnlyList<IFrontendCoreModule> frontendModules,
            IReadOnlyList<IAirOptimizer> optimizers,
            IAbstractIR air)
        {
        }

        public void AfterOptimizedAir(
            CompilationInput input,
            IReadOnlyList<IFrontendCoreModule> frontendModules,
            IReadOnlyList<IAirOptimizer> optimizers,
            IAbstractIR air)
        {
        }
    }
}
