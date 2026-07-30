namespace UniversalToolchain.ModuleContracts;

public sealed class ModuleContractPipelineObserver : ICompilationPipelineObserver
{
    private readonly IBytecodeObservedEmissionReader _observedEmissionReader;
    private readonly IBytecodeVerifier _bytecodeVerifier;
    private readonly IAirVerifier _airVerifier;
    private readonly ModuleContractPipelineOptions _options;
    private readonly ISelectedModuleContractTableProvider _tableProvider;
    private readonly IBackendCapabilitySelectionFactory _backendSelectionFactory;
    private readonly IModuleContractDiagnosticPolicy _diagnosticPolicy;
    private readonly PipelineEffectVerifier _pipelineEffectVerifier;
    private readonly CompilerFactVerifierRegistry _factVerifierRegistry;
    private readonly ICompilerStageFactSeedProvider _stageFactSeedProvider;

    public ModuleContractPipelineObserver(
        ModuleContractPipelineOptions options,
        ISelectedModuleContractTableProvider tableProvider,
        IBytecodeObservedEmissionReader observedEmissionReader,
        IBytecodeVerifier bytecodeVerifier,
        IAirVerifier airVerifier,
        IBackendCapabilitySelectionFactory backendSelectionFactory,
        IModuleContractDiagnosticPolicy diagnosticPolicy,
        PipelineEffectVerifier pipelineEffectVerifier,
        CompilerFactVerifierRegistry factVerifierRegistry,
        ICompilerStageFactSeedProvider stageFactSeedProvider)
    {
        _options = options.ArgNotNull();
        _tableProvider = tableProvider.ArgNotNull();
        _observedEmissionReader = observedEmissionReader.ArgNotNull();
        _bytecodeVerifier = bytecodeVerifier.ArgNotNull();
        _airVerifier = airVerifier.ArgNotNull();
        _backendSelectionFactory = backendSelectionFactory.ArgNotNull();
        _diagnosticPolicy = diagnosticPolicy.ArgNotNull();
        _pipelineEffectVerifier = pipelineEffectVerifier.ArgNotNull();
        _factVerifierRegistry = factVerifierRegistry.ArgNotNull();
        _stageFactSeedProvider = stageFactSeedProvider.ArgNotNull();
    }

    public void AfterBytecode(CompilationPipelineBytecodeContext context)
    {
        context = context.ArgNotNull();
        if (!_options.Enabled)
            return;

        var report = _tableProvider.Build(context.FrontendModules, [], context.BackendComponents ?? []);
        if (report == null)
            return;

        _diagnosticPolicy.ReportAndThrowIfErrors("module contract selection", report.Diagnostics);
        var pipelineValidation = _pipelineEffectVerifier.Validate(new PipelineEffectValidationRequest(
            report.ContractTable,
            CompilerPipelineStage.Bytecode,
            _stageFactSeedProvider.CreateInitialState(CompilerPipelineStage.Bytecode),
            _factVerifierRegistry,
            BuildPipelineOrder(context.FrontendModules, [], context.BackendComponents ?? [])));
        _diagnosticPolicy.ReportAndThrowIfErrors("bytecode pipeline effect verification", pipelineValidation.Diagnostics);
        _diagnosticPolicy.ReportAndThrowIfErrors(
            "bytecode pipeline reverification routing",
            CreateUnhandledReverificationDiagnostics(
                pipelineValidation,
                KnownCoreVerifierRules.BytecodeContract,
                _options.BytecodeProfile));

        var readResult = _observedEmissionReader.ReadWithDiagnostics(context.Bytecode);
        _diagnosticPolicy.ReportAndThrowIfErrors("bytecode contract metadata", readResult.Diagnostics);
        var verification = _bytecodeVerifier.Verify(new BytecodeVerificationRequest(
            context.Bytecode,
            report.ContractTable,
            _options.BytecodeProfile,
            readResult.ObservedEmissions));

        _diagnosticPolicy.ReportAndThrowIfErrors("bytecode contract verification", verification.Diagnostics);
    }

    public void AfterAir(CompilationPipelineAirContext context)
    {
        context = context.ArgNotNull();
        VerifyAir(context, "AIR contract verification");
    }

    public void AfterOptimizedAir(CompilationPipelineAirContext context)
    {
        context = context.ArgNotNull();
        VerifyAir(context, "optimized AIR contract verification");
    }

    private void VerifyAir(CompilationPipelineAirContext context, string stage)
    {
        if (!_options.Enabled)
            return;

        var report = _tableProvider.Build(context.FrontendModules, context.Optimizers, context.BackendComponents ?? []);
        if (report == null)
            return;

        var pipelineStage = stage.Contains("optimized", StringComparison.OrdinalIgnoreCase)
            ? CompilerPipelineStage.OptimizedAir
            : CompilerPipelineStage.Air;

        _diagnosticPolicy.ReportAndThrowIfErrors("module contract selection", report.Diagnostics);
        var pipelineValidation = _pipelineEffectVerifier.Validate(new PipelineEffectValidationRequest(
            report.ContractTable,
            pipelineStage,
            _stageFactSeedProvider.CreateInitialState(pipelineStage),
            _factVerifierRegistry,
            BuildPipelineOrder(context.FrontendModules, context.Optimizers, context.BackendComponents ?? [])));
        _diagnosticPolicy.ReportAndThrowIfErrors("AIR pipeline effect verification", pipelineValidation.Diagnostics);
        _diagnosticPolicy.ReportAndThrowIfErrors(
            "AIR pipeline reverification routing",
            CreateUnhandledReverificationDiagnostics(
                pipelineValidation,
                KnownCoreVerifierRules.AirContract,
                _options.AirProfile));

        BackendCapabilitySelection backendSelection;
        try
        {
            backendSelection = _backendSelectionFactory.Create(
                report.ContractTable,
                context.CompilerSupportedIntrinsics);
        }
        catch (InvalidOperationException exception)
        {
            _diagnosticPolicy.ReportAndThrowIfErrors(
                "backend capability selection",
                [
                    new ToolchainDiagnostic(
                        ModuleContractDiagnosticCodes.MultipleBackendCapabilityFacets,
                        ToolchainDiagnosticSeverity.Error,
                        exception.Message,
                        null,
                        [new ToolchainDiagnosticHint("Build the selected contract table from exactly one runtime-selected backend component.")])
                ]);
            throw;
        }
        var verification = _airVerifier.Verify(new AirVerificationRequest(
            context.Air,
            report.ContractTable,
            backendSelection,
            _options.AirProfile));

        _diagnosticPolicy.ReportAndThrowIfErrors(stage, verification.Diagnostics);
    }

    private static IReadOnlyList<ToolchainDiagnostic> CreateUnhandledReverificationDiagnostics(
        PipelineEffectValidationResult validationResult,
        VerifierRuleId handledRule,
        VerificationSeverityProfile profile)
    {
        var severity = VerificationSeveritySelector.Select(profile);
        return validationResult.ReverificationRequests
            .Where(request => request.RuleId != handledRule)
            .Select(request => new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.CompilerFactReverificationRequired,
                severity,
                $"Pipeline effects invalidated facts requiring verifier '{request.RuleId}', but the current observer stage only runs '{handledRule}'.",
                null,
                [
                    new ToolchainDiagnosticHint(
                        "Route this invalidation to the matching verifier stage or stop invalidating facts owned by a different semantic boundary."),
                    new ToolchainDiagnosticHint(
                        $"Invalidated facts: {string.Join(", ", request.InvalidatedFacts.Select(static fact => fact.Value))}.")
                ]))
            .OrderBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<ModuleId> BuildPipelineOrder(
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IAirOptimizer> optimizers,
        IReadOnlyList<IBackendPipelineComponent> backendComponents)
    {
        return frontendModules
            .Cast<object>()
            .Concat(optimizers)
            .SelectMany(SelectedModuleContractTableProvider.ReadSelectedModuleIds)
            .Concat(backendComponents
                .OfType<IModuleContractBackendPipelineComponent>()
                .SelectMany(SelectedModuleContractTableProvider.ReadSelectedBackendModuleIds))
            .Concat([
                KnownCoreModuleIds.CompilerFacts,
                KnownCoreModuleIds.BackendCapabilities
            ])
            .ToArray();
    }
}
