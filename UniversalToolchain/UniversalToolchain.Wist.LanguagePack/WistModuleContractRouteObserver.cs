using System.Collections.Concurrent;
using AbstractIrConverters;
using BasicCilCompiler.Contracts;
using BasicCore.Contracts;
using BasicInterpreter.Contracts;
using BytecodeDynamicMethodsCompiler.Compilers;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// Adapts the canonical LanguagePlan artifact route to the existing module-contract verifier.
/// Verification observes already-selected route components; it never performs planning, discovery,
/// runtime-profile overlay, or backend selection. Contract components are metadata-only projections
/// of the exact contributions already captured by <see cref="LanguagePlan"/>.
/// </summary>
internal sealed class WistModuleContractRouteObserver : ILanguageArtifactRouteListener
{
    private readonly ModuleContractPipelineOptions _options;
    private readonly IModuleContractDiagnosticSink _sink;
    private readonly ConcurrentDictionary<string, ModuleContractPipelineObserver> _observers =
        new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, IReadOnlyList<IFrontendCoreModule>> _frontendContracts =
        new(StringComparer.Ordinal);

    private WistModuleContractRouteObserver(ModuleContractVerificationOptions verification)
    {
        var snapshot = (verification ?? throw new ArgumentNullException(nameof(verification))).SnapshotValidated();
        _options = snapshot.PipelineOptions;
        _sink = snapshot.DiagnosticSink;
    }

    public static LanguageRuntimeOptions CreateRuntimeOptions(
        IEnumerable<System.Reflection.Assembly>? allowedAssemblies,
        ModuleContractVerificationOptions verification)
    {
        var options = new LanguageRuntimeOptions(allowedAssemblies);
        options.AddRouteListener(new WistModuleContractRouteObserver(verification));
        return options;
    }

    private ModuleContractPipelineObserver GetObserver(LanguagePlan plan) =>
        _observers.GetOrAdd(plan.PlanHash, _ => CreateObserver(plan));

    private IReadOnlyList<IFrontendCoreModule> GetFrontendContractComponents(LanguagePlan plan) =>
        _frontendContracts.GetOrAdd(plan.PlanHash, _ => CreateFrontendContractComponents(plan));

    private ModuleContractPipelineObserver CreateObserver(LanguagePlan plan) =>
        new(
            _options,
            new SelectedModuleContractTableProvider(_options.EnforcementPolicy, new ModuleContractSelectionBuilder()),
            new BytecodeObservedEmissionReader(),
            new BytecodeVerifier(),
            CreateAirVerifier(plan),
            new BackendCapabilitySelectionFactory(_options.BackendPolicy),
            new ModuleContractDiagnosticPolicy(_sink),
            new PipelineEffectVerifier(),
            CompilerFactVerifierRegistry.Core,
            new CoreCompilerStageFactSeedProvider());

    private static AirVerifier CreateAirVerifier(LanguagePlan plan)
    {
        var catalog = new IntrinsicCatalogBuilder().Build(
            WistRuntimeComponentCatalog.CreateSelectedIntrinsicDescriptorProviders(plan));
        return new AirVerifier(
            new InstructionIntrinsicReader(),
            new IntrinsicTypeStackProcessor(catalog, new IntrinsicTypeResolutionContext()));
    }

    public void AfterTransformation(LanguageArtifactRouteObservationContext observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var observer = GetObserver(observation.Plan);
        var frontendContracts = GetFrontendContractComponents(observation.Plan);
        var backendComponents = CreateBackendComponents(observation.Backend);

        if (observation.Step.ContributionId == WistContributionIds.LoweringToBytecode &&
            observation.Artifact is LanguageArtifact<WistBytecodeArtifact> bytecodeArtifact)
        {
            var bytecode = bytecodeArtifact.Value;
            observer.AfterBytecode(new(
                bytecode.Input,
                frontendContracts,
                bytecode.Bytecode,
                backendComponents));
            return;
        }

        if (observation.Artifact is not LanguageArtifact<WistAirArtifact> airArtifact)
            return;

        var air = airArtifact.Value;
        var appliedOptimizerContracts = CreateAppliedOptimizerContractComponents(observation);
        var supportedIntrinsics = GetSupportedIntrinsics(observation.Backend);
        if (observation.Step.ContributionId == WistContributionIds.LoweringToAir)
        {
            observer.AfterAir(new(
                air.Input,
                frontendContracts,
                appliedOptimizerContracts,
                air.Air,
                supportedIntrinsics,
                backendComponents));
        }

        if (!HasLaterAirPass(observation))
        {
            observer.AfterOptimizedAir(new(
                air.Input,
                frontendContracts,
                appliedOptimizerContracts,
                air.Air,
                supportedIntrinsics,
                backendComponents));
        }
    }

    private static IReadOnlyList<IFrontendCoreModule> CreateFrontendContractComponents(LanguagePlan plan) =>
        plan.Contributions
            .Where(static contribution => WistRuntimeComponentCatalog.IsCanonicalModule(contribution.Contribution.Id))
            .Select(static contribution => WistRuntimeComponentCatalog.GetRequired(
                contribution.Contribution.Id,
                WistRuntimeComponentKind.Module))
            .Select(static component => (IFrontendCoreModule)new PlannedFrontendContractComponent(
                CreateContractDescriptorProvider(component)))
            .ToArray();

    private static IReadOnlyList<IAirOptimizer> CreateAppliedOptimizerContractComponents(
        LanguageArtifactRouteObservationContext observation)
    {
        var optimizerIds = WistRuntimeComponentCatalog.Optimizers
            .Select(static component => component.ContributionId)
            .ToHashSet();
        return observation.RouteSteps
            .Take(observation.StepIndex + 1)
            .Select(static step => step.ContributionId)
            .Where(optimizerIds.Contains)
            .Select(static contributionId => WistRuntimeComponentCatalog.GetRequired(
                contributionId,
                WistRuntimeComponentKind.Optimizer))
            .Select(static component => (IAirOptimizer)new PlannedOptimizerContractComponent(
                CreateContractDescriptorProvider(component)))
            .ToArray();
    }

    private static IModuleContractDescriptorProvider CreateContractDescriptorProvider(
        WistRuntimeComponentDescriptor component)
    {
        if (component.ContributionId == WistContributionIds.NumbersModule)
            return new NumbersModule.Contracts.NumbersModuleContractDescriptorProvider();
        if (component.ContributionId == WistContributionIds.ScopesModule)
            return new ScopesModule.Contracts.ScopesModuleContractDescriptorProvider();
        if (component.ContributionId == WistContributionIds.IdentifiersModule)
            return new IdentifierModule.Contracts.IdentifierModuleContractDescriptorProvider();
        if (component.ContributionId == WistContributionIds.VariablesModule)
            return new VariablesModule.Contracts.VariablesModuleContractDescriptorProvider();
        if (component.ContributionId == WistContributionIds.LabelsModule)
            return new LabelsModule.Contracts.LabelsModuleContractDescriptorProvider();
        return new DeclaredRuntimeComponentContractDescriptorProvider(component.ImplementationType);
    }

    private static bool HasLaterAirPass(LanguageArtifactRouteObservationContext observation) =>
        observation.RouteSteps
            .Skip(observation.StepIndex + 1)
            .Any(step => step.SourceContract == WistArtifactKinds.AirContract &&
                         step.TargetContract == WistArtifactKinds.AirContract);

    private static IReadOnlyList<IBackendPipelineComponent> CreateBackendComponents(BackendId backend)
    {
        var supportedIntrinsics = GetSupportedIntrinsics(backend);
        if (backend.Value == "cil")
        {
            return
            [
                new ModuleContractBackendPipelineComponent(
                    CilBackendContractDescriptorProvider.Module.Value,
                    [new CilBackendContractDescriptorProvider(supportedIntrinsics)])
            ];
        }
        if (backend.Value == "interpreter")
        {
            return
            [
                new ModuleContractBackendPipelineComponent(
                    InterpreterBackendContractDescriptorProvider.Module.Value,
                    [new InterpreterBackendContractDescriptorProvider(supportedIntrinsics)])
            ];
        }

        throw new InvalidOperationException($"Unsupported Wist backend '{backend.Value}'.");
    }

    private static IReadOnlyList<string> GetSupportedIntrinsics(BackendId backend) => backend.Value switch
    {
        "cil" => new AbstractMethodsCompilerImpl().SupportedIntrinsics,
        "interpreter" => AbstractIrToAbstractIrStub.SupportedIntrinsicIds,
        _ => throw new InvalidOperationException($"Unsupported Wist backend '{backend.Value}'.")
    };

    private sealed class PlannedFrontendContractComponent(IModuleContractDescriptorProvider provider) :
        IFrontendCoreModule,
        IModuleContractDescriptorProvider
    {
        private readonly IModuleContractDescriptorProvider _provider =
            provider ?? throw new ArgumentNullException(nameof(provider));

        public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => _provider.NamespaceOwners;
        public IReadOnlyList<IModuleContractFacet> GetFacets() => _provider.GetFacets();
    }

    private sealed class PlannedOptimizerContractComponent(IModuleContractDescriptorProvider provider) :
        IAirOptimizer,
        IModuleContractDescriptorProvider
    {
        private readonly IModuleContractDescriptorProvider _provider =
            provider ?? throw new ArgumentNullException(nameof(provider));

        public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => _provider.NamespaceOwners;
        public IReadOnlyList<IModuleContractFacet> GetFacets() => _provider.GetFacets();
    }
}
