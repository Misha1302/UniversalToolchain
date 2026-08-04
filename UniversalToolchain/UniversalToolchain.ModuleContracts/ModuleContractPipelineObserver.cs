using System.Runtime.CompilerServices;
using BasicCore.Compilation;

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

    private static readonly IReadOnlyList<ModuleContractVerifierRoute> BackendInputRoutes =
    [
        new(KnownCoreVerifierRules.BackendInputContract, "core.backend-input")
    ];

    private readonly ConditionalWeakTable<CompilationInput, CompilationLifecycleState> _states = new();
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

        try
        {
            var state = _states.GetValue(context.Input, static _ => new CompilationLifecycleState());
            lock (state.Gate)
            {
                AdvanceState(state, CompilerPipelineStage.Bytecode);

                var report = _tableProvider.Build(context.FrontendModules, [], context.BackendComponents ?? []);
                if (report == null)
                    return;

                _diagnosticPolicy.ReportAndThrowIfErrors("module contract selection", report.Diagnostics);
                var pipelineValidation = ValidatePipelineEffects(
                    report.ContractTable,
                    CompilerPipelineStage.Bytecode,
                    state,
                    context.FrontendModules,
                    [],
                    context.BackendComponents ?? []);
                _diagnosticPolicy.ReportAndThrowIfErrors(
                    "bytecode pipeline effect verification",
                    pipelineValidation.Diagnostics);
                StoreValidation(state, pipelineValidation);
                ReportUnhandledReverificationIfEnforcing(
                    "bytecode pipeline reverification routing",
                    pipelineValidation,
                    CompilerPipelineStage.Bytecode,
                    KnownCoreVerifierRules.BytecodeContract,
                    _options.BytecodeProfile);

                var scheduled = Schedule(
                    CompilerPipelineStage.Bytecode,
                    BytecodeRoutes,
                    state.PendingObligations);

                var readResult = _observedEmissionReader.ReadWithDiagnostics(context.Bytecode);
                _diagnosticPolicy.ReportAndThrowIfErrors("bytecode contract metadata", readResult.Diagnostics);

                // Bytecode establishes its first semantic baseline once. A due obligation is discharged
                // only when the selected policy scheduled the canonical route; passive invalidation
                // therefore remains observably pending even though the baseline checker also ran.
                var verification = _bytecodeVerifier.Verify(new BytecodeVerificationRequest(
                    context.Bytecode,
                    report.ContractTable,
                    _options.BytecodeProfile,
                    readResult.ObservedEmissions));
                _diagnosticPolicy.ReportAndThrowIfErrors("bytecode contract verification", verification.Diagnostics);
                ApplyVerifierSuccess(
                    state,
                    KnownCoreVerifierRules.BytecodeContract,
                    CompilerPipelineStage.Bytecode,
                    scheduled,
                    establishesBaseline: true);
            }
        }
        catch
        {
            _states.Remove(context.Input);
            throw;
        }
    }

    public void AfterAir(CompilationPipelineAirContext context)
    {
        context = context.ArgNotNull();
        try
        {
            VerifyAirBoundary(
                context,
                CompilerPipelineStage.Air,
                "AIR contract verification",
                AirRoutes,
                KnownCoreVerifierRules.AirContract,
                runStructural: true,
                establishesBaseline: true);
        }
        catch
        {
            _states.Remove(context.Input);
            throw;
        }
    }

    public void AfterOptimizedAir(CompilationPipelineAirContext context)
    {
        context = context.ArgNotNull();
        try
        {
            VerifyAirBoundary(
                context,
                CompilerPipelineStage.OptimizedAir,
                "optimized AIR contract verification",
                AirRoutes,
                KnownCoreVerifierRules.AirContract,
                runStructural: true,
                establishesBaseline: false);
        }
        catch
        {
            _states.Remove(context.Input);
            throw;
        }
    }

    public void BeforeBackend(CompilationPipelineAirContext context)
    {
        context = context.ArgNotNull();
        try
        {
            VerifyAirBoundary(
                context,
                CompilerPipelineStage.BackendInput,
                "backend-input contract verification",
                BackendInputRoutes,
                KnownCoreVerifierRules.BackendInputContract,
                runStructural: false,
                establishesBaseline: false);
        }
        finally
        {
            _states.Remove(context.Input);
        }
    }

    private void VerifyAirBoundary(
        CompilationPipelineAirContext context,
        CompilerPipelineStage pipelineStage,
        string stage,
        IReadOnlyList<ModuleContractVerifierRoute> availableRoutes,
        VerifierRuleId handledRule,
        bool runStructural,
        bool establishesBaseline)
    {
        if (!_options.Enabled)
            return;

        if (_options.VerificationPolicy == ModuleContractVerificationPolicy.P0Structural)
        {
            if (!runStructural)
                return;

            var p0Report = _tableProvider.Build(
                context.FrontendModules,
                context.Optimizers,
                context.BackendComponents ?? []);
            if (p0Report == null)
                return;

            var p0Selection = CreateBackendSelection(
                p0Report.ContractTable,
                context.CompilerSupportedIntrinsics);
            VerifyAirScope(
                context,
                p0Report.ContractTable,
                p0Selection,
                AirVerificationScope.Structural,
                stage);
            return;
        }

        var state = _states.GetValue(context.Input, static _ => new CompilationLifecycleState());
        lock (state.Gate)
        {
            AdvanceState(state, pipelineStage);

            var report = _tableProvider.Build(
                context.FrontendModules,
                context.Optimizers,
                context.BackendComponents ?? []);
            if (report == null)
                return;

            _diagnosticPolicy.ReportAndThrowIfErrors("module contract selection", report.Diagnostics);
            var pipelineValidation = ValidatePipelineEffects(
                report.ContractTable,
                pipelineStage,
                state,
                context.FrontendModules,
                context.Optimizers,
                context.BackendComponents ?? []);
            _diagnosticPolicy.ReportAndThrowIfErrors(
                $"{stage} pipeline effect verification",
                pipelineValidation.Diagnostics);
            StoreValidation(state, pipelineValidation);
            ReportUnhandledReverificationIfEnforcing(
                $"{stage} pipeline reverification routing",
                pipelineValidation,
                pipelineStage,
                handledRule,
                _options.AirProfile);

            var scheduled = Schedule(
                pipelineStage,
                availableRoutes,
                state.PendingObligations);
            var backendSelection = CreateBackendSelection(
                report.ContractTable,
                context.CompilerSupportedIntrinsics);

            if (runStructural)
            {
                VerifyAirScope(
                    context,
                    report.ContractTable,
                    backendSelection,
                    AirVerificationScope.Structural,
                    stage);
            }

            var matchingInvocations = scheduled
                .Where(invocation => invocation.RuleId == handledRule)
                .ToArray();
            foreach (var invocation in scheduled.Where(invocation => invocation.RuleId != handledRule))
            {
                throw new InvalidOperationException(
                    $"Boundary '{pipelineStage}' cannot execute verifier '{invocation.RuleId}' " +
                    $"owned by '{invocation.CanonicalOwner}'.");
            }

            if (establishesBaseline || matchingInvocations.Length > 0)
            {
                VerifyAirScope(
                    context,
                    report.ContractTable,
                    backendSelection,
                    AirVerificationScope.Semantic,
                    stage);
                ApplyVerifierSuccess(
                    state,
                    handledRule,
                    pipelineStage,
                    matchingInvocations,
                    establishesBaseline);
            }

            if (pipelineStage == CompilerPipelineStage.BackendInput &&
                (_options.VerificationPolicy is ModuleContractVerificationPolicy.P2Selective or
                    ModuleContractVerificationPolicy.P3Always) &&
                state.PendingObligations.Count != 0)
            {
                throw new InvalidOperationException(
                    "Compilation reached the final modeled boundary with undischarged semantic " +
                    $"verification obligations: {FormatObligations(state.PendingObligations)}.");
            }
        }
    }

    private IReadOnlyList<ModuleContractScheduledVerifierInvocation> Schedule(
        CompilerPipelineStage boundary,
        IReadOnlyList<ModuleContractVerifierRoute> availableRoutes,
        IReadOnlyList<VerificationObligation> obligations) =>
        ModuleContractVerificationScheduler.Schedule(
            _options.VerificationPolicy,
            boundary,
            availableRoutes,
            obligations,
            _options.DemandedFacts,
            _factVerifierRegistry.KnownFacts);

    private PipelineEffectValidationResult ValidatePipelineEffects(
        SelectedModuleContractTable contractTable,
        CompilerPipelineStage stage,
        CompilationLifecycleState state,
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IAirOptimizer> optimizers,
        IReadOnlyList<IBackendPipelineComponent> backendComponents) =>
        _pipelineEffectVerifier.Validate(new PipelineEffectValidationRequest(
            contractTable,
            stage,
            state.Facts,
            _factVerifierRegistry,
            BuildPipelineOrder(frontendModules, optimizers, backendComponents),
            state.PendingObligations));

    private void AdvanceState(
        CompilationLifecycleState state,
        CompilerPipelineStage boundary)
    {
        if (state.LastBoundary is { } previousBoundary && boundary <= previousBoundary)
        {
            throw new InvalidOperationException(
                $"Compilation pipeline boundary '{boundary}' was observed after '{previousBoundary}'. " +
                "Boundary order must be strictly increasing for obligation lifecycle tracking.");
        }

        var seed = _stageFactSeedProvider.CreateInitialState(boundary);
        var available = state.Facts.Available.ToHashSet();
        var invalidated = state.Facts.Invalidated.ToHashSet();

        foreach (var fact in seed.Available)
        {
            if (!invalidated.Contains(fact))
                available.Add(fact);
        }

        foreach (var fact in seed.Invalidated)
        {
            available.Remove(fact);
            invalidated.Add(fact);
        }

        state.Facts = new CompilerFactState(available, invalidated);
        state.LastBoundary = boundary;
    }

    private static void StoreValidation(
        CompilationLifecycleState state,
        PipelineEffectValidationResult validation)
    {
        state.Facts = validation.OutputFacts;
        state.PendingObligations = validation.VerificationObligations.ToList();
    }

    private void ApplyVerifierSuccess(
        CompilationLifecycleState state,
        VerifierRuleId ruleId,
        CompilerPipelineStage boundary,
        IReadOnlyList<ModuleContractScheduledVerifierInvocation> scheduledInvocations,
        bool establishesBaseline)
    {
        var scheduledFacts = scheduledInvocations
            .SelectMany(static invocation => invocation.InvalidatedFacts)
            .ToHashSet();
        var establishedFacts = new HashSet<CompilerFactId>(scheduledFacts);

        if (establishesBaseline ||
            scheduledInvocations.Any(static invocation => !invocation.IsObligationDriven))
        {
            foreach (var fact in _factVerifierRegistry.GetFactsForRoute(ruleId, boundary))
            {
                var hasUndischargedObligation = state.PendingObligations.Any(
                    obligation =>
                        obligation.FactId == fact &&
                        !scheduledFacts.Contains(fact));
                if (!hasUndischargedObligation)
                    establishedFacts.Add(fact);
            }
        }

        if (establishedFacts.Count == 0)
            return;

        var available = state.Facts.Available.ToHashSet();
        var invalidated = state.Facts.Invalidated.ToHashSet();
        foreach (var fact in establishedFacts)
        {
            available.Add(fact);
            invalidated.Remove(fact);
        }

        state.PendingObligations.RemoveAll(
            obligation =>
                obligation.RuleId == ruleId &&
                obligation.FirstEligibleBoundary <= boundary &&
                establishedFacts.Contains(obligation.FactId));
        state.Facts = new CompilerFactState(available, invalidated);
    }

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

    private void ReportUnhandledReverificationIfEnforcing(
        string stage,
        PipelineEffectValidationResult validationResult,
        CompilerPipelineStage currentBoundary,
        VerifierRuleId handledRule,
        VerificationSeverityProfile profile)
    {
        if (_options.VerificationPolicy is not (
                ModuleContractVerificationPolicy.P2Selective or
                ModuleContractVerificationPolicy.P3Always))
        {
            return;
        }

        _diagnosticPolicy.ReportAndThrowIfErrors(
            stage,
            CreateUnhandledReverificationDiagnostics(
                validationResult,
                currentBoundary,
                handledRule,
                profile));
    }

    private static IReadOnlyList<ToolchainDiagnostic> CreateUnhandledReverificationDiagnostics(
        PipelineEffectValidationResult validationResult,
        CompilerPipelineStage currentBoundary,
        VerifierRuleId handledRule,
        VerificationSeverityProfile profile)
    {
        var severity = VerificationSeveritySelector.Select(profile);
        return validationResult.VerificationObligations
            .Where(obligation => obligation.FirstEligibleBoundary <= currentBoundary)
            .GroupBy(static obligation => obligation.RuleId)
            .Where(group => group.Key != handledRule)
            .Select(group => new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.CompilerFactReverificationRequired,
                severity,
                $"Pipeline effects created due obligations for verifier '{group.Key}', but boundary " +
                $"'{currentBoundary}' only runs '{handledRule}'.",
                null,
                [
                    new ToolchainDiagnosticHint(
                        "Route this obligation to an executable canonical verifier at its first eligible boundary."),
                    new ToolchainDiagnosticHint(
                        $"Invalidated facts: {string.Join(", ", group.Select(static obligation => obligation.FactId.Value))}.")
                ]))
            .OrderBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static string FormatObligations(IEnumerable<VerificationObligation> obligations) =>
        string.Join(
            ", ",
            obligations
                .OrderBy(static obligation => obligation.FirstEligibleBoundary)
                .ThenBy(static obligation => obligation.RuleId.Value, StringComparer.Ordinal)
                .ThenBy(static obligation => obligation.FactId.Value, StringComparer.Ordinal)
                .Select(static obligation =>
                    $"{obligation.FactId}@{obligation.FirstEligibleBoundary}/{obligation.CanonicalOwner}"));

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

    private sealed class CompilationLifecycleState
    {
        public object Gate { get; } = new();

        public CompilerFactState Facts { get; set; } = CompilerFactState.Empty;

        public List<VerificationObligation> PendingObligations { get; set; } = [];

        public CompilerPipelineStage? LastBoundary { get; set; }
    }
}
