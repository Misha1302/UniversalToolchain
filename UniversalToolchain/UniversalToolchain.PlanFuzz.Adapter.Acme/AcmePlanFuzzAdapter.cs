namespace UniversalToolchain.PlanFuzz.Adapter.Acme;

/// <summary>
/// Implements the first independent non-Wist PlanFuzz vertical slice.
/// </summary>
public sealed class AcmePlanFuzzAdapter : IPlanFuzzLanguageAdapter, IPlanFuzzProgramReducer
{
    private readonly AcmePricingProgramGenerator _generator = new();

    public PlanFuzzAdapterDescriptor Descriptor { get; } = new(
        AcmePlanFuzzConstants.AdapterId,
        AcmePlanFuzzConstants.AdapterVersion,
        AcmePlanFuzzConstants.LanguageId,
        AcmePlanFuzzConstants.GeneratorSchemaVersion,
        ["backend-parity", "plan-determinism", "canonical-lock", "negative-surface", "extension-noninterference", "seeded-fault", "program-reduction"]);

    public PlanFuzzTestCase GenerateCase(
        ulong campaignSeed,
        long caseIndex,
        PlanFuzzCaseGenerationOptions options)
    {
        options = options.ArgNotNull();
        if (caseIndex < 0)
            return Thrower.Argument<PlanFuzzTestCase>(nameof(caseIndex), "Case index must not be negative.");
        if (options.SeededFaultId != null &&
            !StringComparer.Ordinal.Equals(options.SeededFaultId, AcmePlanFuzzConstants.WrongArithmeticFault) &&
            !StringComparer.Ordinal.Equals(options.SeededFaultId, AcmePlanFuzzConstants.ExcludedActivationFault) &&
            !StringComparer.Ordinal.Equals(options.SeededFaultId, AcmePlanFuzzConstants.ExtensionInterferenceFault))
        {
            return Thrower.NotSupported<PlanFuzzTestCase>($"Unknown Acme seeded fault '{options.SeededFaultId}'.");
        }

        var caseSeed = new PlanFuzzRandom(campaignSeed).Fork($"case:{caseIndex.ToString(CultureInfo.InvariantCulture)}").NextUInt64();
        var programModel = _generator.Generate(new PlanFuzzRandom(caseSeed).Fork("program"));
        var program = new PlanFuzzProgram(
            AcmePlanFuzzConstants.ModelKind,
            AcmePlanFuzzConstants.ModelSchemaVersion,
            programModel.ToPayload(),
            programModel.RenderSource(),
            PlanFuzzProgramClass.ValidDeterministic);

        var variants = new List<PlanFuzzPlanVariant>
        {
            new(
                "baseline.interpreter",
                AcmePlanFuzzConstants.BaselineConfiguration,
                AcmePlanFuzzConstants.InterpreterBackend,
                PlanFuzzVariantRole.Baseline,
                PlanFuzzExpectedRelation.SameSemantics),
            new(
                "baseline.compiled",
                AcmePlanFuzzConstants.BaselineConfiguration,
                AcmePlanFuzzConstants.CompiledBackend,
                PlanFuzzVariantRole.Baseline,
                PlanFuzzExpectedRelation.SameSemantics),
            new(
                "registry-reversed.interpreter",
                AcmePlanFuzzConstants.ReversedRegistryConfiguration,
                AcmePlanFuzzConstants.InterpreterBackend,
                PlanFuzzVariantRole.EquivalentMutation,
                PlanFuzzExpectedRelation.SameSemantics,
                AcmePlanFuzzConstants.RegistryOrderMutation),
            new(
                "registry-reversed.compiled",
                AcmePlanFuzzConstants.ReversedRegistryConfiguration,
                AcmePlanFuzzConstants.CompiledBackend,
                PlanFuzzVariantRole.EquivalentMutation,
                PlanFuzzExpectedRelation.SameSemantics,
                AcmePlanFuzzConstants.RegistryOrderMutation),
            new(
                "independent-extension.interpreter",
                AcmePlanFuzzConstants.IndependentExtensionConfiguration,
                AcmePlanFuzzConstants.InterpreterBackend,
                PlanFuzzVariantRole.EquivalentMutation,
                PlanFuzzExpectedRelation.SameSemantics,
                AcmePlanFuzzConstants.IndependentExtensionMutation),
            new(
                "independent-extension.compiled",
                AcmePlanFuzzConstants.IndependentExtensionConfiguration,
                AcmePlanFuzzConstants.CompiledBackend,
                PlanFuzzVariantRole.EquivalentMutation,
                PlanFuzzExpectedRelation.SameSemantics,
                AcmePlanFuzzConstants.IndependentExtensionMutation)
        };

        var oracleContracts = new List<PlanFuzzOracleContract>
        {
            new(
                "backend-parity.baseline",
                PlanFuzzOracleIds.BackendParity,
                1,
                ["baseline.interpreter", "baseline.compiled"]),
            new(
                "plan-determinism.interpreter",
                PlanFuzzOracleIds.PlanDeterminism,
                1,
                ["baseline.interpreter", "registry-reversed.interpreter"]),
            new(
                "plan-determinism.compiled",
                PlanFuzzOracleIds.PlanDeterminism,
                1,
                ["baseline.compiled", "registry-reversed.compiled"]),
            new(
                "negative-surface.baseline",
                PlanFuzzOracleIds.NegativeSurfacePreservation,
                1,
                ["baseline.interpreter", "baseline.compiled"]),
            new(
                "extension-noninterference.interpreter",
                PlanFuzzOracleIds.ExtensionNoninterference,
                1,
                ["baseline.interpreter", "independent-extension.interpreter"]),
            new(
                "extension-noninterference.compiled",
                PlanFuzzOracleIds.ExtensionNoninterference,
                1,
                ["baseline.compiled", "independent-extension.compiled"]),
            new(
                "canonical-lock.all",
                PlanFuzzOracleIds.CanonicalLockConsistency,
                1,
                variants.Select(static variant => variant.VariantId))
        };

        if (StringComparer.Ordinal.Equals(options.SeededFaultId, AcmePlanFuzzConstants.WrongArithmeticFault))
        {
            variants.Add(new PlanFuzzPlanVariant(
                "seeded-wrong-arithmetic.compiled",
                AcmePlanFuzzConstants.WrongArithmeticConfiguration,
                AcmePlanFuzzConstants.CompiledBackend,
                PlanFuzzVariantRole.SeededFault,
                PlanFuzzExpectedRelation.ExpectedDifference,
                seededFaultId: AcmePlanFuzzConstants.WrongArithmeticFault));
            oracleContracts.Add(new PlanFuzzOracleContract(
                "backend-parity.seeded-wrong-arithmetic",
                PlanFuzzOracleIds.BackendParity,
                1,
                ["baseline.interpreter", "seeded-wrong-arithmetic.compiled"]));
        }
        else if (StringComparer.Ordinal.Equals(options.SeededFaultId, AcmePlanFuzzConstants.ExcludedActivationFault))
        {
            variants.Add(new PlanFuzzPlanVariant(
                "seeded-excluded-activation.interpreter",
                AcmePlanFuzzConstants.ExcludedActivationConfiguration,
                AcmePlanFuzzConstants.InterpreterBackend,
                PlanFuzzVariantRole.SeededFault,
                PlanFuzzExpectedRelation.ExpectedDifference,
                seededFaultId: AcmePlanFuzzConstants.ExcludedActivationFault));
            oracleContracts.Add(new PlanFuzzOracleContract(
                "negative-surface.seeded-excluded-activation",
                PlanFuzzOracleIds.NegativeSurfacePreservation,
                1,
                ["seeded-excluded-activation.interpreter"]));
        }
        else if (StringComparer.Ordinal.Equals(options.SeededFaultId, AcmePlanFuzzConstants.ExtensionInterferenceFault))
        {
            variants.Add(new PlanFuzzPlanVariant(
                "seeded-extension-interference.interpreter",
                AcmePlanFuzzConstants.ExtensionInterferenceConfiguration,
                AcmePlanFuzzConstants.InterpreterBackend,
                PlanFuzzVariantRole.SeededFault,
                PlanFuzzExpectedRelation.ExpectedDifference,
                seededFaultId: AcmePlanFuzzConstants.ExtensionInterferenceFault));
            oracleContracts.Add(new PlanFuzzOracleContract(
                "extension-noninterference.seeded",
                PlanFuzzOracleIds.ExtensionNoninterference,
                1,
                ["baseline.interpreter", "seeded-extension-interference.interpreter"]));
        }

        oracleContracts.RemoveAll(static contract => StringComparer.Ordinal.Equals(contract.ContractId, "canonical-lock.all"));
        oracleContracts.Add(new PlanFuzzOracleContract(
            "canonical-lock.all",
            PlanFuzzOracleIds.CanonicalLockConsistency,
            1,
            variants.Select(static variant => variant.VariantId)));

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
            oracleContracts);
    }

    public long GetProgramComplexity(PlanFuzzTestCase testCase)
    {
        testCase = testCase.ArgNotNull();
        EnsureReductionSchema(testCase);
        return AcmePricingProgramReducer.GetComplexity(
            AcmePricingProgramModel.FromPayload(testCase.Program.Model));
    }

    public IReadOnlyList<PlanFuzzProgramReductionCandidate> GetProgramReductionCandidates(
        PlanFuzzTestCase testCase)
    {
        testCase = testCase.ArgNotNull();
        EnsureReductionSchema(testCase);
        var model = AcmePricingProgramModel.FromPayload(testCase.Program.Model);
        if (!StringComparer.Ordinal.Equals(model.RenderSource(), testCase.Program.SourceText))
            return Thrower.InvalidOpEx<IReadOnlyList<PlanFuzzProgramReductionCandidate>>(
                "Structured Acme program model does not reproduce the recorded source text.");
        return AcmePricingProgramReducer.CreateCandidates(testCase, model);
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
                "Testcase adapter identity does not match the Acme adapter.");
        }
        if (!StringComparer.Ordinal.Equals(testCase.Program.ModelKind, AcmePlanFuzzConstants.ModelKind) ||
            testCase.Program.ModelSchemaVersion != AcmePlanFuzzConstants.ModelSchemaVersion)
        {
            return PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "program-schema",
                "Testcase program model is not supported by the Acme adapter.");
        }

        AcmePricingProgramModel model;
        try
        {
            model = AcmePricingProgramModel.FromPayload(testCase.Program.Model);
        }
        catch (Exception exception)
        {
            return PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "program-payload",
                exception.Message);
        }
        if (!StringComparer.Ordinal.Equals(model.RenderSource(), testCase.Program.SourceText))
        {
            return PlanFuzzObservation.InfrastructureFailure(
                testCase.CaseId,
                variant,
                "source-projection",
                "Structured program model does not reproduce the recorded source text.");
        }

        var wrongArithmetic = StringComparer.Ordinal.Equals(
            variant.ConfigurationId,
            AcmePlanFuzzConstants.WrongArithmeticConfiguration);
        var includeIndependentExtension =
            StringComparer.Ordinal.Equals(variant.ConfigurationId, AcmePlanFuzzConstants.IndependentExtensionConfiguration) ||
            StringComparer.Ordinal.Equals(variant.ConfigurationId, AcmePlanFuzzConstants.ExtensionInterferenceConfiguration);
        var seedExcludedActivation = StringComparer.Ordinal.Equals(
            variant.ConfigurationId,
            AcmePlanFuzzConstants.ExcludedActivationConfiguration);
        var seedExtensionInterference = StringComparer.Ordinal.Equals(
            variant.ConfigurationId,
            AcmePlanFuzzConstants.ExtensionInterferenceConfiguration);
        var targetPackage = AcmePricingLanguagePackageFactory.Create(wrongArithmetic);
        var unrelatedPackage = AcmePricingLanguagePackageFactory.CreateUnrelated();
        var registry = new LanguagePackageRegistry();
        if (StringComparer.Ordinal.Equals(variant.ConfigurationId, AcmePlanFuzzConstants.ReversedRegistryConfiguration))
        {
            registry.AddPackage(unrelatedPackage);
            registry.AddPackage(targetPackage);
        }
        else
        {
            registry.AddPackage(targetPackage);
            registry.AddPackage(unrelatedPackage);
        }

        LanguagePlan plan;
        try
        {
            plan = new LanguageCompiler(registry)
                .Compile(AcmePricingLanguagePackageFactory.CreateDefinition(includeIndependentExtension))
                .GetRequiredPlan();
        }
        catch (Exception exception)
        {
            return ProgramFailure(testCase, variant, "planning", exception);
        }

        var planSnapshot = CreatePlanSnapshot(plan);
        var surfaceSnapshot = CreateSurfaceSnapshot(plan, variant.BackendId, includeIndependentExtension, seedExcludedActivation);
        try
        {
            using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { targetPackage });
            var result = runtime.Run(new LanguageExecutionRequest(
                testCase.Program.SourceText,
                new BackendId(variant.BackendId)));
            if (result.Value is not decimal decimalValue)
            {
                return PlanFuzzObservation.InfrastructureFailure(
                    testCase.CaseId,
                    variant,
                    "value-type",
                    $"Acme backend returned '{result.Value?.GetType().FullName ?? "null"}' instead of decimal.");
            }
            if (seedExtensionInterference)
                decimalValue += 1m;
            return new PlanFuzzObservation(
                testCase.CaseId,
                variant.VariantId,
                variant.BackendId,
                PlanFuzzExecutionOutcome.Success,
                PlanFuzzValueSnapshot.FromDecimal(decimalValue),
                null,
                planSnapshot,
                null,
                surfaceSnapshot);
        }
        catch (Exception exception)
        {
            return new PlanFuzzObservation(
                testCase.CaseId,
                variant.VariantId,
                variant.BackendId,
                PlanFuzzExecutionOutcome.ProgramFailure,
                null,
                new PlanFuzzFailureSnapshot(
                    exception.GetType().FullName ?? exception.GetType().Name,
                    "execution",
                    "exception",
                    exception.Message),
                planSnapshot,
                null,
                surfaceSnapshot);
        }
    }

    private void EnsureReductionSchema(PlanFuzzTestCase testCase)
    {
        if (!StringComparer.Ordinal.Equals(testCase.AdapterId, Descriptor.AdapterId) ||
            !StringComparer.Ordinal.Equals(testCase.AdapterVersion, Descriptor.AdapterVersion))
        {
            Thrower.Argument(nameof(testCase), "Testcase adapter identity does not match the Acme reducer.");
        }
        if (!StringComparer.Ordinal.Equals(testCase.Program.ModelKind, AcmePlanFuzzConstants.ModelKind) ||
            testCase.Program.ModelSchemaVersion != AcmePlanFuzzConstants.ModelSchemaVersion)
        {
            Thrower.Argument(nameof(testCase), "Testcase program model is not supported by the Acme reducer.");
        }
    }

    private static PlanFuzzObservation ProgramFailure(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant,
        string stage,
        Exception exception) =>
        new(
            testCase.CaseId,
            variant.VariantId,
            variant.BackendId,
            PlanFuzzExecutionOutcome.ProgramFailure,
            null,
            new PlanFuzzFailureSnapshot(
                exception.GetType().FullName ?? exception.GetType().Name,
                stage,
                "exception",
                exception.Message),
            null);

    private static PlanFuzzPlanSnapshot CreatePlanSnapshot(LanguagePlan plan)
    {
        var canonicalBytes = LanguageLockFile.SerializeCanonical(plan);
        var repeatedCanonicalBytes = LanguageLockFile.SerializeCanonical(plan);
        var pretty = LanguageLockFile.Serialize(plan);
        var canonicalText = Encoding.UTF8.GetString(canonicalBytes);
        using var canonicalDocument = JsonDocument.Parse(canonicalBytes);
        return new PlanFuzzPlanSnapshot(
            plan.PlanHash,
            ComputeSha256(canonicalBytes),
            ComputeSha256(repeatedCanonicalBytes),
            PlanFuzzJson.ComputeSha256(canonicalText),
            PlanFuzzJson.ComputeSha256(pretty),
            canonicalDocument.RootElement.GetProperty("schemaVersion").GetInt32(),
            canonicalDocument.RootElement.GetProperty("canonicalization").GetString().NotNull());
    }

    private static PlanFuzzSurfaceSnapshot CreateSurfaceSnapshot(
        LanguagePlan plan,
        string backendId,
        bool includeIndependentExtension,
        bool seedExcludedActivation)
    {
        var backend = new BackendId(backendId);
        var route = plan.Routes[backend];
        var backendOwner = plan.Contributions.Single(item =>
            item.Contribution.Slot == LanguageSlots.Backends &&
            item.Contribution.SupportedBackends.Contains(backend) &&
            item.Contribution.BackendInputContract == route.TargetContract);
        var selected = plan.Features
            .Select(static item => SurfaceFeature(item.Feature.Id.Value))
            .Concat(plan.Contributions.Select(static item => SurfaceContribution(item.Contribution.Id.Value)))
            .ToArray();
        var independent = includeIndependentExtension
            ? IndependentSurfaceIds()
            : [];
        var excluded = includeIndependentExtension
            ? []
            : IndependentSurfaceIds();
        var activated = route.Steps
            .Select(static step => SurfaceContribution(step.ContributionId.Value))
            .Append(SurfaceContribution(backendOwner.Contribution.Id.Value))
            .Concat(plan.RuntimeProviderContribution == null
                ? []
                : [SurfaceContribution(plan.RuntimeProviderContribution.Contribution.Id.Value)])
            .ToList();
        if (seedExcludedActivation)
            activated.Add(SurfaceContribution(AcmePlanFuzzConstants.IndependentContributionId));

        return new PlanFuzzSurfaceSnapshot(
            selected,
            excluded,
            independent,
            activated,
            activationTraceComplete: true,
            traceKind: "language-route-runtime-v1",
            routeIdentity: CreateRouteIdentity(route, backendOwner.Contribution.Id));
    }

    private static string[] IndependentSurfaceIds() =>
    [
        SurfaceFeature(AcmePlanFuzzConstants.IndependentFeatureId),
        SurfaceContribution(AcmePlanFuzzConstants.IndependentContributionId)
    ];

    private static string SurfaceFeature(string id) => $"feature:{id}";
    private static string SurfaceContribution(string id) => $"contribution:{id}";

    private static string CreateRouteIdentity(LanguageArtifactRoute route, LanguageContributionId backendOwner) =>
        $"backend:{route.Backend.Value}|source:{DescribeContract(route.SourceContract)}|steps:{string.Join(',', route.Steps.Select(static step => step.ContributionId.Value))}|target:{DescribeContract(route.TargetContract)}|executor:{backendOwner.Value}";

    private static string DescribeContract(LanguageArtifactContract contract) =>
        $"{contract.Kind.Value}@{contract.ValueTypeIdentity ?? "untyped"}";

    private static string ComputeSha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
