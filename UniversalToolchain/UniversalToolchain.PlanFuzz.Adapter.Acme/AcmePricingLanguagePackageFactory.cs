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

    public static AuthoredLanguagePackage Create(bool wrongArithmetic)
    {
        var builder = LanguagePackageBuilder.Create(AcmePlanFuzzConstants.LanguageId, AcmePlanFuzzConstants.LanguageVersion)
            .AddFeature(AcmePlanFuzzConstants.CoreFeatureId, feature => ConfigureFeature(feature, wrongArithmetic))
            .AddFeature(AcmePlanFuzzConstants.IndependentFeatureId, ConfigureIndependentFeature)
            .UseRouteRuntime("acme.pricing.runtime", AcmePlanFuzzConstants.LanguageVersion);
        return builder.Build();
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

    private static void ConfigureIndependentFeature(LanguageFeatureBuilder feature) =>
        feature.AddTransformer(
            AcmePlanFuzzConstants.IndependentContributionId,
            IndependentSlot,
            IndependentInput,
            IndependentOutput,
            static (value, _) => value,
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
            cost: 1);

    private static void ConfigureFeature(LanguageFeatureBuilder feature, bool wrongArithmetic)
    {
        feature
            .AddTransformer(
                "acme.pricing.parse",
                LanguageSlots.FrontendParser,
                StandardLanguageArtifactKinds.SourceText,
                Syntax,
                static (source, _) => AcmePricingExpression.Parse(source),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                cost: 1);

        if (wrongArithmetic)
        {
            feature.AddTransformer(
                "acme.pricing.compile",
                LanguageSlots.Lowering,
                Syntax,
                Executable,
                static (expression, _) => expression.CompileWrongArithmetic(),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                cost: 1,
                supportedBackends: [Compiled]);
        }
        else
        {
            feature.AddTransformer(
                "acme.pricing.compile",
                LanguageSlots.Lowering,
                Syntax,
                Executable,
                static (expression, _) => expression.Compile(),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                cost: 1,
                supportedBackends: [Compiled]);
        }

        feature
            .AddBackend(
                Interpreter,
                new LanguageContributionId("acme.pricing.interpreter"),
                Syntax,
                static (expression, _) => expression.Evaluate(),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
            .AddBackend(
                Compiled,
                new LanguageContributionId("acme.pricing.compiled"),
                Executable,
                static (program, _) => program(),
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop);
    }
}
