namespace UniversalToolchain.PlanFuzz.Adapter.Wist;

/// <summary>
/// Implements PlanFuzz Phase 1 for the documented Wist restricted Int32 arithmetic subset.
/// </summary>
public sealed class WistPlanFuzzAdapter : IPlanFuzzLanguageAdapter
{
    private static readonly IReadOnlySet<string> ClassifiedUnsupportedDiagnostics = new HashSet<string>(StringComparer.Ordinal)
    {
        "air.to-ssa.stack-underflow",
        "air.to-ssa.return-arity",
        "air.to-ssa.return-type",
        "air.to-ssa.push-type",
        "air.to-ssa.opcode",
        "air.to-ssa.managed-call.projection.unregistered",
        "ssa.optimization.managed-call.binding.missing",
        "ssa.to-air.managed-call.binding.missing",
        "ssa.to-air.value-reuse.unsupported"
    };

    private readonly WistIntProgramGenerator _generator = new();

    public PlanFuzzAdapterDescriptor Descriptor { get; } = new(
        WistPlanFuzzConstants.AdapterId,
        WistPlanFuzzConstants.AdapterVersion,
        WistPlanFuzzConstants.LanguageId,
        WistPlanFuzzConstants.GeneratorSchemaVersion,
        ["backend-parity", "optimization-route-parity", "controlled-fallback", "restricted-int32", "regression-corpus"]);

    public PlanFuzzTestCase GenerateCase(
        ulong campaignSeed,
        long caseIndex,
        PlanFuzzCaseGenerationOptions options)
    {
        options = options.ArgNotNull();
        if (caseIndex < 0)
            return Thrower.Argument<PlanFuzzTestCase>(nameof(caseIndex), "Case index must not be negative.");
        if (options.SeededFaultId != null)
            return Thrower.NotSupported<PlanFuzzTestCase>("The Wist Level 0 adapter does not expose seeded faults yet.");

        var caseSeed = new PlanFuzzRandom(campaignSeed)
            .Fork($"case:{caseIndex.ToString(CultureInfo.InvariantCulture)}")
            .NextUInt64();
        return CreateCase(
            campaignSeed,
            caseIndex,
            caseSeed,
            _generator.Generate(
                new PlanFuzzRandom(caseSeed).Fork("program"),
                caseIndex,
                options.IncludeRegressionCorpus));
    }

    public PlanFuzzTestCase CreateCase(
        ulong campaignSeed,
        long caseIndex,
        ulong caseSeed,
        WistIntProgramModel model)
    {
        model = model.ArgNotNull();
        var program = new PlanFuzzProgram(
            WistPlanFuzzConstants.ModelKind,
            WistPlanFuzzConstants.ModelSchemaVersion,
            model.ToPayload(),
            model.RenderSource(),
            PlanFuzzProgramClass.ValidDeterministic);

        var variants = new[]
        {
            new PlanFuzzPlanVariant(
                "interpreter.disabled",
                WistPlanFuzzConstants.DisabledConfiguration,
                WistPlanFuzzConstants.InterpreterBackend,
                PlanFuzzVariantRole.Baseline,
                PlanFuzzExpectedRelation.SameSemantics),
            new PlanFuzzPlanVariant(
                "compiler.disabled",
                WistPlanFuzzConstants.DisabledConfiguration,
                WistPlanFuzzConstants.CompilerBackend,
                PlanFuzzVariantRole.Baseline,
                PlanFuzzExpectedRelation.SameSemantics),
            new PlanFuzzPlanVariant(
                "compiler.ssa-prefer",
                WistPlanFuzzConstants.PreferConfiguration,
                WistPlanFuzzConstants.CompilerBackend,
                PlanFuzzVariantRole.EquivalentMutation,
                PlanFuzzExpectedRelation.SameSemantics,
                WistPlanFuzzConstants.SsaPreferMutation),
            new PlanFuzzPlanVariant(
                "compiler.ssa-require",
                WistPlanFuzzConstants.RequireConfiguration,
                WistPlanFuzzConstants.CompilerBackend,
                PlanFuzzVariantRole.EquivalentMutation,
                PlanFuzzExpectedRelation.SameSemantics,
                WistPlanFuzzConstants.SsaRequireMutation)
        };

        var contracts = new[]
        {
            new PlanFuzzOracleContract(
                "backend-parity.disabled",
                PlanFuzzOracleIds.BackendParity,
                1,
                ["interpreter.disabled", "compiler.disabled"]),
            new PlanFuzzOracleContract(
                "route-parity.prefer",
                PlanFuzzOracleIds.OptimizationRouteParity,
                1,
                ["compiler.disabled", "compiler.ssa-prefer"]),
            new PlanFuzzOracleContract(
                "route-parity.require",
                PlanFuzzOracleIds.OptimizationRouteParity,
                1,
                ["compiler.disabled", "compiler.ssa-require"]),
            new PlanFuzzOracleContract(
                "controlled-fallback.prefer",
                PlanFuzzOracleIds.ControlledFallback,
                1,
                ["compiler.ssa-prefer"])
        };

        return new PlanFuzzTestCase(
            PlanFuzzConstants.CaseSchemaVersion,
            Descriptor.AdapterId,
            Descriptor.AdapterVersion,
            campaignSeed,
            caseIndex,
            caseSeed,
            PlanFuzzRandom.AlgorithmId,
            program,
            variants,
            contracts);
    }

    public PlanFuzzObservation Execute(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant)
    {
        testCase = testCase.ArgNotNull();
        variant = variant.ArgNotNull();
        if (!StringComparer.Ordinal.Equals(testCase.AdapterId, Descriptor.AdapterId) ||
            !StringComparer.Ordinal.Equals(testCase.AdapterVersion, Descriptor.AdapterVersion))
        {
            return PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "adapter-identity",
                "Testcase adapter identity does not match the Wist adapter.");
        }
        if (!StringComparer.Ordinal.Equals(testCase.Program.ModelKind, WistPlanFuzzConstants.ModelKind) ||
            testCase.Program.ModelSchemaVersion != WistPlanFuzzConstants.ModelSchemaVersion)
        {
            return PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "program-schema",
                "Testcase program model is not supported by the Wist adapter.");
        }

        var variantFailure = ValidateVariant(testCase, variant);
        if (variantFailure != null)
            return variantFailure;

        WistIntProgramModel model;
        try
        {
            model = WistIntProgramModel.FromPayload(testCase.Program.Model);
        }
        catch (Exception exception)
        {
            return PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "program-payload",
                Bound(exception.Message));
        }
        if (!StringComparer.Ordinal.Equals(model.RenderSource(), testCase.Program.SourceText))
        {
            return PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "source-projection",
                "Structured Wist program model does not reproduce the recorded source text.");
        }

        if (StringComparer.Ordinal.Equals(variant.BackendId, WistPlanFuzzConstants.InterpreterBackend))
            return ExecuteInterpreter(testCase, variant, model);
        if (StringComparer.Ordinal.Equals(variant.BackendId, WistPlanFuzzConstants.CompilerBackend))
            return ExecuteCompiler(testCase, variant, model);
        return PlanFuzzObservation.InfrastructureFailure(
            testCase.CaseId,
            variant,
            "variant-backend",
            $"Unknown Wist PlanFuzz backend '{variant.BackendId}'.");
    }

    private static PlanFuzzObservation? ValidateVariant(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant)
    {
        PlanFuzzPlanVariant declared;
        try
        {
            declared = testCase.GetRequiredVariant(variant.VariantId);
        }
        catch (InvalidOperationException exception)
        {
            return PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "variant-identity",
                Bound(exception.Message));
        }

        if (!StringComparer.Ordinal.Equals(declared.BackendId, variant.BackendId) ||
            !StringComparer.Ordinal.Equals(declared.ConfigurationId, variant.ConfigurationId))
        {
            return PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "variant-identity",
                "Execution variant does not match the testcase-declared backend and configuration.");
        }

        if (StringComparer.Ordinal.Equals(variant.BackendId, WistPlanFuzzConstants.InterpreterBackend))
        {
            return StringComparer.Ordinal.Equals(variant.ConfigurationId, WistPlanFuzzConstants.DisabledConfiguration)
                ? null
                : PlanFuzzObservation.InfrastructureFailure(
                    testCase.CaseId,
                    variant,
                    "variant-configuration",
                    "The Wist interpreter variant only supports the SSA-disabled configuration.");
        }

        if (!StringComparer.Ordinal.Equals(variant.BackendId, WistPlanFuzzConstants.CompilerBackend))
        {
            return PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "variant-backend",
                $"Unknown Wist PlanFuzz backend '{variant.BackendId}'.");
        }

        var supported = StringComparer.Ordinal.Equals(variant.ConfigurationId, WistPlanFuzzConstants.DisabledConfiguration) ||
                        StringComparer.Ordinal.Equals(variant.ConfigurationId, WistPlanFuzzConstants.PreferConfiguration) ||
                        StringComparer.Ordinal.Equals(variant.ConfigurationId, WistPlanFuzzConstants.RequireConfiguration);
        return supported
            ? null
            : PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "variant-configuration",
                $"Unknown Wist PlanFuzz compiler configuration '{variant.ConfigurationId}'.");
    }

    private static PlanFuzzObservation ExecuteInterpreter(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant,
        WistIntProgramModel model)
    {
        var route = DisabledRoute();
        try
        {
            using var engine = WistEngine.Create(new WistEngineOptions
            {
                Preset = WistPreset.RestrictedArithmetic,
                Backend = WistBackend.Interpreter,
                Optimization = CreateOptimization(WistSsaPolicy.Disabled)
            });
            var value = model.UsesParameter
                ? engine.Evaluate<int>(
                    testCase.Program.SourceText,
                    new Dictionary<string, object?> { ["x"] = model.ParameterValue })
                : engine.Evaluate<int>(testCase.Program.SourceText);
            return Success(testCase, variant, value, route);
        }
        catch (Exception exception)
        {
            return Failure(
                testCase,
                variant,
                exception.GetType().FullName ?? exception.GetType().Name,
                "execution",
                "wist.interpreter.failure",
                exception.Message,
                route);
        }
    }

    private static PlanFuzzObservation ExecuteCompiler(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant,
        WistIntProgramModel model)
    {
        var policy = ResolvePolicy(variant.ConfigurationId);
        using var engine = WistEngine.Create(new WistEngineOptions
        {
            Preset = WistPreset.RestrictedArithmetic,
            Backend = WistBackend.Compiler,
            Optimization = CreateOptimization(policy)
        });

        if (model.UsesParameter)
        {
            var result = engine.TryCompile<Func<int, int>>(testCase.Program.SourceText, "x");
            var route = CreateRoute(result.OptimizationReport.Ssa);
            if (!result.IsSuccess)
                return CompilationFailure(testCase, variant, result.Diagnostics, result.Exception, route);
            try
            {
                return Success(testCase, variant, result.Program!.CompiledDelegate(model.ParameterValue), route);
            }
            catch (Exception exception)
            {
                return Failure(
                    testCase,
                    variant,
                    exception.GetType().FullName ?? exception.GetType().Name,
                    "execution",
                    "wist.compiled-invocation.failure",
                    exception.Message,
                    route);
            }
        }

        var parameterlessResult = engine.TryCompile<Func<int>>(testCase.Program.SourceText);
        var parameterlessRoute = CreateRoute(parameterlessResult.OptimizationReport.Ssa);
        if (!parameterlessResult.IsSuccess)
            return CompilationFailure(testCase, variant, parameterlessResult.Diagnostics, parameterlessResult.Exception, parameterlessRoute);
        try
        {
            return Success(testCase, variant, parameterlessResult.Program!.CompiledDelegate(), parameterlessRoute);
        }
        catch (Exception exception)
        {
            return Failure(
                testCase,
                variant,
                exception.GetType().FullName ?? exception.GetType().Name,
                "execution",
                "wist.compiled-invocation.failure",
                exception.Message,
                parameterlessRoute);
        }
    }

    private static WistOptimizationOptions CreateOptimization(WistSsaPolicy policy) => new()
    {
        Ssa = new WistSsaOptions
        {
            Policy = policy,
            DiagnosticLevel = WistSsaDiagnosticLevel.Detailed
        }
    };

    private static WistSsaPolicy ResolvePolicy(string configurationId)
    {
        if (StringComparer.Ordinal.Equals(configurationId, WistPlanFuzzConstants.DisabledConfiguration))
            return WistSsaPolicy.Disabled;
        if (StringComparer.Ordinal.Equals(configurationId, WistPlanFuzzConstants.PreferConfiguration))
            return WistSsaPolicy.Prefer;
        if (StringComparer.Ordinal.Equals(configurationId, WistPlanFuzzConstants.RequireConfiguration))
            return WistSsaPolicy.Require;
        return Thrower.NotSupported<WistSsaPolicy>($"Unknown Wist PlanFuzz configuration '{configurationId}'.");
    }

    private static PlanFuzzObservation Success(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant,
        int value,
        PlanFuzzRouteSnapshot route) =>
        new(
            testCase.CaseId,
            variant.VariantId,
            variant.BackendId,
            PlanFuzzExecutionOutcome.Success,
            PlanFuzzValueSnapshot.FromInt32(value),
            null,
            null,
            route);

    private static PlanFuzzObservation CompilationFailure(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant,
        IReadOnlyList<WistDiagnostic> diagnostics,
        Exception? exception,
        PlanFuzzRouteSnapshot route)
    {
        var diagnostic = diagnostics.FirstOrDefault(static item => item.Severity == WistDiagnosticSeverity.Error)
            ?? diagnostics.FirstOrDefault();
        return Failure(
            testCase,
            variant,
            exception?.GetType().FullName ?? "wist-compilation-failure",
            diagnostic?.Stage ?? "compilation",
            diagnostic?.Code ?? "wist.compilation.failure",
            diagnostic?.Message ?? exception?.Message ?? "Wist compilation failed without a structured diagnostic.",
            route);
    }

    private static PlanFuzzObservation Failure(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant,
        string failureType,
        string stage,
        string category,
        string message,
        PlanFuzzRouteSnapshot route) =>
        new(
            testCase.CaseId,
            variant.VariantId,
            variant.BackendId,
            PlanFuzzExecutionOutcome.ProgramFailure,
            null,
            new PlanFuzzFailureSnapshot(failureType, stage, category, Bound(message)),
            null,
            route);

    private static PlanFuzzRouteSnapshot DisabledRoute() =>
        new(
            WistPlanFuzzConstants.SsaRouteId,
            WistSsaPolicy.Disabled.ToString(),
            usedRoute: false,
            fellBack: false,
            PlanFuzzFallbackKind.None);

    private static PlanFuzzRouteSnapshot CreateRoute(WistSsaOptimizationReport report)
    {
        report = report.ArgNotNull();
        var fallbackKind = PlanFuzzFallbackKind.None;
        if (report.FellBackToAir)
        {
            fallbackKind = report.Diagnostics.Count > 0 &&
                           report.Diagnostics.All(diagnostic => ClassifiedUnsupportedDiagnostics.Contains(diagnostic.Code))
                ? PlanFuzzFallbackKind.ClassifiedUnsupportedShape
                : PlanFuzzFallbackKind.Unclassified;
        }

        return new PlanFuzzRouteSnapshot(
            WistPlanFuzzConstants.SsaRouteId,
            report.RequestedPolicy.ToString(),
            report.UsedSsa,
            report.FellBackToAir,
            fallbackKind,
            report.Profile,
            report.InputAirInstructionCount,
            report.OutputAirInstructionCount,
            report.ExecutedPasses,
            report.Diagnostics.Select(static diagnostic =>
                new PlanFuzzRouteDiagnosticSnapshot(diagnostic.Code, diagnostic.Stage)));
    }

    private static string Bound(string value)
    {
        value ??= string.Empty;
        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        return normalized.Length <= 2_048 ? normalized : normalized[..2_048];
    }
}
