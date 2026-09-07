using DslEvolutionStudy;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace DslEvolutionStudy.Rq3;

internal static class UtDownstreamTreatment
{
    private static readonly BackendId Backend = new("rq3.interpreter");
    private static readonly LanguageArtifactKind<PricingProgram> ProgramArtifact = new("rq3.pricing.program");

    public static Rq3Result Evaluate(string source, IReadOnlyList<string> features)
    {
        var package = BuildPackage();
        var definition = LanguageDefinitionBuilder.Create("DslEvolution.Rq3", "1.0.0")
            .UseFeature("rq3.core")
            .EnableBackend(Backend)
            .UseRuntimeProvider("rq3.runtime", "1.0.0");
        foreach (var feature in features)
            definition.UseFeature($"rq3.{feature}");

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package)).Compile(definition.Build());
        if (!result.IsSuccess)
            throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
        using var runtime = LanguageRuntime.Create(result.GetRequiredPlan(), new ILanguageRouteComponentSource[] { package });
        return (Rq3Result)(runtime.Run(new LanguageExecutionRequest(source, Backend)).Value
            ?? throw new InvalidOperationException("RQ3 backend returned null."));
    }

    private static AuthoredLanguagePackage BuildPackage() =>
        LanguagePackageBuilder.Create("DslEvolution.Rq3", "1.0.0")
            .AddFeature("rq3.core", feature => feature
                .AddTransformer("rq3.parse", LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText, ProgramArtifact,
                    static (source, _) => PricingParser.Parse(source),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop, cost: 1)
                .AddPass("rq3.downstream.canonicalize", LanguageSlots.Optimizers, ProgramArtifact,
                    static (program, _) => Canonicalize(program),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop, order: 100)
                .AddBackend(Backend, new LanguageContributionId("rq3.interpreter"), ProgramArtifact,
                    static (program, _) => new Rq3Result(program.Value, program.Operations.Count),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .AddFeature("rq3.discount", feature => feature
                .Requires(new LanguageFeatureId("rq3.core"))
                .AddPass("rq3.discount.apply", LanguageSlots.Optimizers, ProgramArtifact,
                    static (program, _) => UtFeatureSemantics.ApplyDiscount(program),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop, order: 10))
            .AddFeature("rq3.surcharge", feature => feature
                .Requires(new LanguageFeatureId("rq3.core"))
                .AddPass("rq3.surcharge.apply", LanguageSlots.Optimizers, ProgramArtifact,
                    static (program, _) => UtFeatureSemantics.ApplySurcharge(program),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop, order: 20))
            .UseRouteRuntime("rq3.runtime", "1.0.0")
            .Build();

    private static PricingProgram Canonicalize(PricingProgram program) =>
        program with { Operations = Array.Empty<PricingOperation>() };
}
