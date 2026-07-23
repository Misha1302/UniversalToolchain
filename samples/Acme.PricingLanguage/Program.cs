using System.Globalization;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

var interpreter = new BackendId("interpreter");
var compiled = new BackendId("compiled");
var syntax = new LanguageArtifactKind<PricingExpression>("acme.pricing.syntax");
var executable = new LanguageArtifactKind<Func<decimal>>("acme.pricing.executable");

var languagePackage = LanguagePackageBuilder.Create("Acme.PricingLanguage", "1.0.0")
    .AddFeature("acme.pricing.core", feature => feature
        .AddTransformer(
            "acme.pricing.parse",
            LanguageSlots.FrontendParser,
            StandardLanguageArtifactKinds.SourceText,
            syntax,
            static (source, _) => PricingExpression.Parse(source),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
            cost: 1)
        .AddTransformer(
            "acme.pricing.compile",
            LanguageSlots.Lowering,
            syntax,
            executable,
            static (expression, _) => expression.Compile(),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
            cost: 1,
            supportedBackends: [compiled])
        .AddBackend(
            interpreter,
            new LanguageContributionId("acme.pricing.interpreter"),
            syntax,
            static (expression, _) => expression.Evaluate(),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
        .AddBackend(
            compiled,
            new LanguageContributionId("acme.pricing.compiled"),
            executable,
            static (program, _) => program(),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
    .UseRouteRuntime("acme.pricing.runtime", "1.0.0")
    .Build();

var registry = new LanguagePackageRegistry().AddPackage(languagePackage);
var definition = LanguageDefinitionBuilder.Create("Acme.PricingLanguage", "1.0.0")
    .UseFeature("acme.pricing.core")
    .EnableBackend(interpreter)
    .EnableBackend(compiled)
    .UseRuntimeProvider("acme.pricing.runtime", "1.0.0")
    .WithRuntimePolicy(new LanguageRuntimePolicy(RequireDeterminism: true, MaximumSourceLength: 256))
    .Build();
var plan = new LanguageCompiler(registry).Compile(definition).GetRequiredPlan();
using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { languagePackage });

const string source = "12.5 * 3 - 2.5";
var interpreted = runtime.Run(new LanguageExecutionRequest(source, interpreter)).Value;
var compiledValue = runtime.Run(new LanguageExecutionRequest(source, compiled)).Value;
Console.WriteLine($"{interpreted}:{compiledValue}");

internal sealed record PricingExpression(decimal UnitPrice, decimal Quantity, decimal Discount)
{
    public static PricingExpression Parse(string source)
    {
        var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 5 || parts[1] != "*" || parts[3] != "-")
            throw new FormatException("Expected: <unit-price> * <quantity> - <discount>.");
        return new PricingExpression(ParseDecimal(parts[0]), ParseDecimal(parts[2]), ParseDecimal(parts[4]));
    }

    public decimal Evaluate() => UnitPrice * Quantity - Discount;
    public Func<decimal> Compile() => Evaluate;

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
}
