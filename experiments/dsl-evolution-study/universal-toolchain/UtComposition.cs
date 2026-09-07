using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace DslEvolutionStudy;

internal sealed class UtPricingRuntime : IDisposable
{
    private static readonly BackendId Interpreter = new("pricing.interpreter");
    private static readonly LanguageArtifactKind<PricingProgram> ProgramArtifact = new("pricing.program");
    private readonly AuthoredLanguagePackage _package;
    private readonly LanguageRuntime _runtime;

    private UtPricingRuntime(AuthoredLanguagePackage package, LanguageRuntime runtime)
    {
        _package = package;
        _runtime = runtime;
    }

    public static UtPricingRuntime Create(params string[] features)
    {
        var package = BuildPackage();
        var definition = LanguageDefinitionBuilder.Create("DslEvolution.Pricing", "1.0.0")
            .UseFeature("pricing.core")
            .EnableBackend(Interpreter)
            .UseRuntimeProvider("pricing.runtime", "1.0.0")
            .WithRuntimePolicy(new LanguageRuntimePolicy(RequireDeterminism: true, MaximumSourceLength: 256));
        foreach (var feature in features)
            definition.UseFeature(feature);

        var registry = new LanguagePackageRegistry().AddPackage(package);
        var result = new LanguageCompiler(registry).Compile(definition.Build());
        if (!result.IsSuccess)
            throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
        var runtime = LanguageRuntime.Create(result.GetRequiredPlan(), new ILanguageRouteComponentSource[] { package });
        return new UtPricingRuntime(package, runtime);
    }

    public decimal Evaluate(string source) =>
        (decimal)(_runtime.Run(new LanguageExecutionRequest(source, Interpreter)).Value
            ?? throw new InvalidOperationException("UT backend returned null."));

    public void Dispose() => _runtime.Dispose();

    private static AuthoredLanguagePackage BuildPackage() =>
        LanguagePackageBuilder.Create("DslEvolution.Pricing", "1.0.0")
            .AddFeature("pricing.core", feature => feature
                .AddTransformer(
                    "pricing.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    ProgramArtifact,
                    static (source, _) => PricingParser.Parse(source),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1)
                .AddBackend(
                    Interpreter,
                    new LanguageContributionId("pricing.interpreter"),
                    ProgramArtifact,
                    static (program, _) => program.Value,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .AddFeature("pricing.discount", feature => feature
                .Requires(new LanguageFeatureId("pricing.core"))
                .AddPass(
                    "pricing.discount.apply",
                    LanguageSlots.Optimizers,
                    ProgramArtifact,
                    static (program, _) => ApplyDiscount(program),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    order: 10))
            .AddFeature("pricing.surcharge", feature => feature
                .Requires(new LanguageFeatureId("pricing.core"))
                .AddPass(
                    "pricing.surcharge.apply",
                    LanguageSlots.Optimizers,
                    ProgramArtifact,
                    static (program, _) => ApplySurcharge(program),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    order: 20))
            .UseRouteRuntime("pricing.runtime", "1.0.0")
            .Build();

    private static PricingProgram ApplyDiscount(PricingProgram program)
    {
        var value = program.Value;
        foreach (var operation in program.Operations.Where(static op => op.Name == "discount"))
        {
            var percent = UtPercentRule.RequireValid(operation.Percent);
            value *= 1m - percent / 100m;
        }
        return program.WithValue(value);
    }

    private static PricingProgram ApplySurcharge(PricingProgram program)
    {
        var value = program.Value;
        foreach (var operation in program.Operations.Where(static op => op.Name == "surcharge"))
        {
            var percent = UtPercentRule.RequireValid(operation.Percent);
            value *= 1m + percent / 100m;
        }
        return program.WithValue(value);
    }
}

internal static class UtPercentRule
{
    public static decimal RequireValid(decimal percent) => percent;
}
