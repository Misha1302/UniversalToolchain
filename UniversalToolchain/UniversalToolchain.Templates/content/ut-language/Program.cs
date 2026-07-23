using System.Globalization;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

var backend = new BackendId("interpreter");
var syntax = new LanguageArtifactKind<LanguageSyntax>("TemplateLanguage.syntax");

var package = LanguagePackageBuilder.Create("TemplateLanguage", "1.0.0")
    .AddFeature("TemplateLanguage.core", feature => feature
        .AddTransformer(
            "TemplateLanguage.parse",
            LanguageSlots.FrontendParser,
            StandardLanguageArtifactKinds.SourceText,
            syntax,
            static (source, _) => new LanguageSyntax(
                int.Parse(source, NumberStyles.Integer, CultureInfo.InvariantCulture)),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
            cost: 1)
        .AddBackend(
            backend,
            new LanguageContributionId("TemplateLanguage.interpreter"),
            syntax,
            static (program, _) => program.Value,
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
    .UseRouteRuntime("TemplateLanguage.runtime", "1.0.0")
    .Build();

var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
    .Compile(LanguageDefinitionBuilder.Create("TemplateLanguage", "1.0.0")
        .UseFeature("TemplateLanguage.core")
        .EnableBackend(backend)
        .UseRuntimeProvider("TemplateLanguage.runtime", "1.0.0")
        .Build())
    .GetRequiredPlan();

using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
Console.WriteLine(runtime.Run(new LanguageExecutionRequest("42", backend)).Value);

internal sealed record LanguageSyntax(int Value);
