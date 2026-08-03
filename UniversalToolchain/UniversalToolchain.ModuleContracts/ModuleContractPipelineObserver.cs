namespace UniversalToolchain.ModuleContracts;

public sealed class ModuleContractPipelineObserver : ICompilationPipelineObserver
{
    private static readonly IReadOnlyList<ModuleContractVerifierRoute> BytecodeRoutes =
    [
        new(KnownCoreVerifierRules.BytecodeContract, "core.bytecode")
    ];

    private static readonly IReadOnlyList<ModuleContractVerifierRoute> AirRoutes =
    [
        new(KnownCoreVerifierRules.AirContract, "core.air")
    ];

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
        if (!_options.Enabled || _options.VerificationPolicy == ModuleContractVerificationPolicy.P0Structural)
            return;

        var report = _tableProvider.Build(context.FrontendModules, [], context.BackendComponents ?? []);
        if (report == null)
            return;

        _diagnosticPolicy.ReportAndThrowIfErrors("module contract selection", report.Diagnostics);
        var pipelineValidation = ValidatePipelineEffects(
            report.ContractTable,
            CompilerPipelineStage.Bytecode,
            context.FrontendModules,
            [],
            context.BackendComponents ?? []);
        _diagnosticPolicy.ReportAndThrowIfErrors("bytecode pipeline effect verification", pipelineValidation.Diagnostics);
        _diagnosticPolicy.ReportAndThrowIfErrors(
            "bytecode pipeline reverification routing",
            CreateUnhandledReverificationDiagnostics(
                pipelineValidation,
                KnownCoreVerifierRules.BytecodeContract,
                _options.BytecodeProfile));

        var readResult = _observedEmissionReader.ReadWithDiagnostics(context.Bytecode);
        _diagnosticPolicy.ReportAndThrowIfErrors("bytecode contract metadata", readResult.Diagnostics);

        // Bytecode is verified once at its first semantic boundary for P1/P2/P3. Matching
        // obligations are therefore discharged by this canonical invocation without a duplicate pass.
        var verification = _bytecodeVerifier.Verify(new BytecodeVerificationRequest(
            context.Bytecode,
            report.ContractTable,
            _options.BytecodeProfile,
            readResult.ObservedEmissions));
        _diagnosticPolicy.ReportAndThrowIfErrors("bytecode contract verification", verification.Diagnostics);

        if (_options.VerificationPolicy is ModuleContractVerificationPolicy.P2Selective or
            ModuleContractVerificationPolicy.P3Always)
        {
            _ = ModuleContractVerificationScheduler.Schedule(
                _options.VerificationPolicy,
                BytecodeRoutes,
                pipelineValidation.ReverificationRequests);
        }
    }

    public void AfterAir(CompilationPipelineAirContext context)
    {
        context = context.ArgNotNull();
        VerifyAir(context, CompilerPipelineStage.Air, "AIR contract verification", isOptimizedBoundary: false);
    }

    public void AfterOptimizedAir(CompilationPipelineAirContext context)
    {
        context = context.ArgNotNull();
        VerifyAir(
            context,
            CompilerPipelineStage.OptimizedAir,
            "optimized AIR contract verification",
            isOptimizedBoundary: true);
    }

    private void VerifyAir(
        CompilationPipelineAirContext context,
        CompilerPipelineStage pipelineStage,
        string stage,
        bool isOptimizedBoundary)
    {
        if (!_options.Enabled)
            return;

        var report = _tableProvider.Build(context.FrontendModules, context.Optimizers, context.BackendComponents ?? []);
        if (report == null)
            return;

        PipelineEffectValidationResult? pipelineValidation = null;
        if (_options.VerificationPolicy != ModuleContractVerificationPolicy.P0Structural)
        {
            _diagnosticPolicy.ReportAndThrowIfErrors("module contract selection", report.Diagnostics);
            pipelineValidation = ValidatePipelineEffects(
                report.ContractTable,
                pipelineStage,
                context.FrontendModules,
                context.Optimizers,
                context.BackendComponents ?? []);
            _diagnosticPolicy.ReportAndThrowIfErrors("AIR pipeline effect verification", pipelineValidation.Diagnostics);
            _diagnosticPolicy.ReportAndThrowIfErrors(
                "AIR pipeline reverification routing",
                CreateUnhandledReverificationDiagnostics(
                    pipelineValidation,
                    KnownCoreVerifierRules.AirContract,
                    _options.AirProfile));
        }

        var backendSelection = CreateBackendSelection(report.ContractTable, context.CompilerSupportedIntrinsics);

        // Structural validity is invariant across policies and is always checked.
        VerifyAirScope(
            context,
            report.ContractTable,
            backendSelection,
            AirVerificationScope.Structural,
            stage);

        if (!isOptimizedBoundary)
        {
            // The first AIR boundary establishes the semantic baseline for every contract-aware policy.
            if (_options.VerificationPolicy != ModuleContractVerificationPolicy.P0Structural)
            {
                VerifyAirScope(
                    context,
                    report.ContractTable,
                    backendSelection,
                    AirVerificationScope.Semantic,
                    stage);
            }

            return;
        }

        var scheduled = ModuleContractVerificationScheduler.Schedule(
            _options.VerificationPolicy,
            pipelineStage,
            AirRoutes,
            pipelineValidation?.VerificationObligations ?? [],
            _options.DemandedFacts,
            _factVerifierRegistry.KnownFacts);
        foreach (var invocation in scheduled)
        {
            if (invocation.RuleId != KnownCoreVerifierRules.AirContract)
            {
                throw new InvalidOperationException(
                    $"Optimized AIR boundary cannot execute verifier '{invocation.RuleId}'.");
            }

            VerifyAirScope(
                context,
                report.ContractTable,
                backendSelection,
                AirVerificationScope.Semantic,
                stage);
        }
    }

    private PipelineEffectValidationResult ValidatePipelineEffects(
        SelectedModuleContractTable contractTable,
        CompilerPipelineStage stage,
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IAirOptimizer> optimizers,
        IReadOnlyList<IBackendPipelineComponent> backendComponents) =>
        _pipelineEffectVerifier.Validate(new PipelineEffectValidationRequest(
            contractTable,
            stage,
            _stageFactSeedProvider.CreateInitialState(stage),
            _factVerifierRegistry,
            BuildPipelineOrder(frontendModules, optimizers, backendComponents)));

    private BackendCapabilitySelection CreateBackendSelection(
        SelectedModuleContractTable contractTable,
        IReadOnlyCollection<string> compilerSupportedIntrinsics)
    {
        try
        {
            return _backendSelectionFactory.Create(contractTable, compilerSupportedIntrinsics.ToArray());
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
    }

    private void VerifyAirScope(
        CompilationPipelineAirContext context,
        SelectedModuleContractTable contractTable,
        BackendCapabilitySelection backendSelection,
        AirVerificationScope scope,
        string stage)
    {
        var verification = _airVerifier.Verify(new AirVerificationRequest(
            context.Air,
            contractTable,
            backendSelection,
            _options.AirProfile,
            scope));
        _diagnosticPolicy.ReportAndThrowIfErrors(stage, verification.Diagnostics);
    }

    private static IReadOnlyList<ToolchainDiagnostic> CreateUnhandledReverificationDiagnostics(
        PipelineEffectValidationResult validationResult,
        VerifierRuleId handledRule,
        VerificationSeverityProfile profile)
    {
        var severity = VerificationSeveritySelector.Select(profile);
        return validationResult.VerificationObligations
            .GroupBy(static obligation => obligation.RuleId)
            .Where(group => group.Key != handledRule)
            .Select(group => new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.CompilerFactReverificationRequired,
                severity,
                $"Pipeline effects created obligations for verifier '{group.Key}', but the current observer stage only runs '{handledRule}'.",
                null,
                [
                    new ToolchainDiagnosticHint(
                        "Route this obligation to the matching verifier stage or stop invalidating facts owned by a different semantic boundary."),
                    new ToolchainDiagnosticHint(
                        $"Invalidated facts: {string.Join(", ", group.Select(static obligation => obligation.FactId.Value))}.")
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
        var order = frontendModules
            .Cast<object>()
            .Concat(optimizers)
            .SelectMany(SelectedModuleContractTableProvider.ReadSelectedModuleIds)
            .Concat(backendComponents
                .OfType<IModuleContractBackendPipelineComponent>()
                .SelectMany(SelectedModuleContractTableProvider.ReadSelectedBackendModuleIds))
            .ToList();
        if (!order.Contains(KnownCoreModuleIds.CompilerFacts))
            order.Add(KnownCoreModuleIds.CompilerFacts);
        if (!order.Contains(KnownCoreModuleIds.BackendCapabilities))
            order.Add(KnownCoreModuleIds.BackendCapabilities);
        return order;
    }
}
