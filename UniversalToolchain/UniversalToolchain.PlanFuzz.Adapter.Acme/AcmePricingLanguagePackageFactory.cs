namespace UniversalToolchain.PlanFuzz.Adapter.Acme;

internal static class AcmePricingLanguagePackageFactory
{
    private static readonly BackendId Interpreter = new(AcmePlanFuzzConstants.InterpreterBackend);
    private static readonly BackendId Compiled = new(AcmePlanFuzzConstants.CompiledBackend);
    private static readonly LanguageArtifactKind<AcmePricingExpression> Syntax = new("acme.pricing.syntax");
    private static readonly LanguageArtifactKind<Func<decimal>> Executable = new("acme.pricing.executable");
    private static readonly LanguageArtifactKind<string> IndependentInput = new("acme.pricing.independent.input");
    private static readonly LanguageArtifactKind<string> IndependentOutput = new("acme.pricing.independent.output");
    private static readonly LanguageSlotId IndependentSlot = new("acme.pricing.independent-slot");
    private static readonly LanguageRuntimeComponentTraits Traits = LanguageRuntimeComponentTraits.DeterministicNoHostInterop;

    public static AuthoredLanguagePackage Create(
        bool wrongArithmetic,
        AcmeActivationTrace trace,
        AcmeIndependentExtensionRuntimeHook independentExtension)
    {
        trace = trace.ArgNotNull();
        independentExtension = independentExtension.ArgNotNull();
        return LanguagePackageBuilder.Create(AcmePlanFuzzConstants.LanguageId, AcmePlanFuzzConstants.LanguageVersion)
            .AddFeature(AcmePlanFuzzConstants.CoreFeatureId, feature => ConfigureFeature(feature, wrongArithmetic, trace))
            .AddFeature(AcmePlanFuzzConstants.IndependentFeatureId, feature => ConfigureIndependentFeature(feature, independentExtension))
            .UseRouteRuntime("acme.pricing.runtime", AcmePlanFuzzConstants.LanguageVersion)
            .Build();
    }

    public static AuthoredLanguagePackage CreateUnrelated() =>
        LanguagePackageBuilder.Create("Acme.UnrelatedLanguage", "1.0.0")
            .AddFeature("acme.unrelated.feature", static _ => { })
            .Build();

    public static LanguageDefinition CreateDefinition(bool includeIndependentExtension = false)
    {
        var builder = LanguageDefinitionBuilder.Create(AcmePlanFuzzConstants.LanguageId, AcmePlanFuzzConstants.LanguageVersion)
            .UseFeature(AcmePlanFuzzConstants.CoreFeatureId)
            .EnableBackend(Interpreter)
            .EnableBackend(Compiled)
            .UseRuntimeProvider("acme.pricing.runtime", AcmePlanFuzzConstants.LanguageVersion)
            .WithRuntimePolicy(new LanguageRuntimePolicy(RequireDeterminism: true, MaximumSourceLength: 256));
        if (includeIndependentExtension)
            builder.UseFeature(AcmePlanFuzzConstants.IndependentFeatureId);
        return builder.Build();
    }

    private static void ConfigureIndependentFeature(
        LanguageFeatureBuilder feature,
        AcmeIndependentExtensionRuntimeHook independentExtension) =>
        feature.AddTransformerFactory(
            AcmePlanFuzzConstants.IndependentContributionId,
            IndependentSlot,
            IndependentInput,
            IndependentOutput,
            _ => new DelegateLanguageArtifactTransformer<string, string>(
                new LanguageContributionId(AcmePlanFuzzConstants.IndependentContributionId),
                IndependentInput,
                IndependentOutput,
                (value, _) => independentExtension.Transform(value),
                Traits),
            Traits,
            cost: 1);

    private static void ConfigureFeature(LanguageFeatureBuilder feature, bool wrongArithmetic, AcmeActivationTrace trace)
    {
        feature.AddTransformerFactory(
            "acme.pricing.parse",
            LanguageSlots.FrontendParser,
            StandardLanguageArtifactKinds.SourceText,
            Syntax,
            _ => new DelegateLanguageArtifactTransformer<string, AcmePricingExpression>(
                new LanguageContributionId("acme.pricing.parse"),
                StandardLanguageArtifactKinds.SourceText,
                Syntax,
                (source, _) =>
                {
                    trace.RecordContribution("acme.pricing.parse");
                    return AcmePricingExpression.Parse(source);
                },
                Traits),
            Traits,
            cost: 1);

        feature.AddTransformerFactory(
            "acme.pricing.compile",
            LanguageSlots.Lowering,
            Syntax,
            Executable,
            _ => new DelegateLanguageArtifactTransformer<AcmePricingExpression, Func<decimal>>(
                new LanguageContributionId("acme.pricing.compile"),
                Syntax,
                Executable,
                (expression, _) =>
                {
                    trace.RecordContribution("acme.pricing.compile");
                    return wrongArithmetic ? expression.CompileWrongArithmetic() : expression.Compile();
                },
                Traits),
            Traits,
            cost: 1,
            supportedBackends: [Compiled]);

        feature
            .AddBackendFactory(
                Interpreter,
                new LanguageContributionId("acme.pricing.interpreter"),
                Syntax,
                _ => new DelegateLanguageArtifactExecutor<AcmePricingExpression, decimal>(
                    new LanguageContributionId("acme.pricing.interpreter"),
                    Interpreter,
                    Syntax,
                    (expression, _) =>
                    {
                        trace.RecordContribution("acme.pricing.interpreter");
                        return expression.Evaluate();
                    },
                    Traits),
                Traits)
            .AddBackendFactory(
                Compiled,
                new LanguageContributionId("acme.pricing.compiled"),
                Executable,
                _ => new DelegateLanguageArtifactExecutor<Func<decimal>, decimal>(
                    new LanguageContributionId("acme.pricing.compiled"),
                    Compiled,
                    Executable,
                    (program, _) =>
                    {
                        trace.RecordContribution("acme.pricing.compiled");
                        return program();
                    },
                    Traits),
                Traits);
    }
}
