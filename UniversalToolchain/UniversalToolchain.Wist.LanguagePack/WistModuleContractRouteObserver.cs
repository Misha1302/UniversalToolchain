using AbstractIrConverters;
using BasicCilCompiler.Contracts;
using BasicCore.Contracts;
using BasicInterpreter.Contracts;
using BytecodeDynamicMethodsCompiler.Compilers;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// Adapts the canonical LanguagePlan artifact route to the existing module-contract verifier.
/// Verification observes already-selected route components; it never performs planning, discovery,
/// runtime-profile overlay, or backend selection.
/// </summary>
internal sealed class WistModuleContractRouteObserver : ILanguageArtifactRouteObserver
{
    private readonly ModuleContractPipelineObserver _observer;

    private WistModuleContractRouteObserver(ModuleContractVerificationOptions verification)
    {
        var snapshot = (verification ?? throw new ArgumentNullException(nameof(verification))).SnapshotValidated();
        var options = snapshot.PipelineOptions;
        var sink = snapshot.DiagnosticSink;
        _observer = new ModuleContractPipelineObserver(
            options,
            new SelectedModuleContractTableProvider(options.EnforcementPolicy, new ModuleContractSelectionBuilder()),
            new BytecodeObservedEmissionReader(),
            new BytecodeVerifier(),
            new AirVerifier(),
            new BackendCapabilitySelectionFactory(options.BackendPolicy),
            new ModuleContractDiagnosticPolicy(sink),
            new PipelineEffectVerifier(),
            CompilerFactVerifierRegistry.Core,
            new CoreCompilerStageFactSeedProvider());
    }

    public static LanguageRuntimeOptions CreateRuntimeOptions(
        IEnumerable<System.Reflection.Assembly>? allowedAssemblies,
        ModuleContractVerificationOptions verification)
    {
        var options = new LanguageRuntimeOptions(allowedAssemblies);
        options.AddRouteObserver(new WistModuleContractRouteObserver(verification));
        return options;
    }

    public void AfterTransformation(LanguageArtifactRouteObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var backendComponents = CreateBackendComponents(observation.Backend);

        if (observation.Step.ContributionId == WistContributionIds.LoweringToBytecode &&
            observation.Artifact is LanguageArtifact<WistBytecodeArtifact> bytecodeArtifact)
        {
            var bytecode = bytecodeArtifact.Value;
            _observer.AfterBytecode(new(
                bytecode.Input,
                bytecode.FrontendModules,
                bytecode.Bytecode,
                backendComponents));
            return;
        }

        if (observation.Artifact is not LanguageArtifact<WistAirArtifact> airArtifact)
            return;

        var air = airArtifact.Value;
        var supportedIntrinsics = GetSupportedIntrinsics(observation.Backend);
        if (observation.Step.ContributionId == WistContributionIds.LoweringToAir)
        {
            _observer.AfterAir(new(
                air.Input,
                air.FrontendModules,
                air.Optimizers,
                air.Air,
                supportedIntrinsics,
                backendComponents));
        }

        if (!HasLaterAirPass(observation))
        {
            _observer.AfterOptimizedAir(new(
                air.Input,
                air.FrontendModules,
                air.Optimizers,
                air.Air,
                supportedIntrinsics,
                backendComponents));
        }
    }

    private static bool HasLaterAirPass(LanguageArtifactRouteObservation observation) =>
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
}
