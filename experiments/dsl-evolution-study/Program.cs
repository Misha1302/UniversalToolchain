using System.Globalization;
using System.Text.Json;
using DslEvolutionStudy;

if (args.Length != 2 || args[0] != "--treatment")
{
    Console.Error.WriteLine("usage: --treatment control|clone-own|universal-toolchain");
    return 2;
}

var treatment = args[1];
var specPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "spec", "cases.json");
if (!File.Exists(specPath))
    specPath = Path.Combine(Directory.GetCurrentDirectory(), "experiments", "dsl-evolution-study", "spec", "cases.json");
var cases = JsonSerializer.Deserialize<List<OracleCase>>(File.ReadAllText(specPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

foreach (var @case in cases)
{
    if (treatment == "control" && @case.Id is not ("base" or "discount" or "discount-invalid"))
        continue;
    try
    {
        var value = treatment switch
        {
            "control" => ControlEvaluator.Evaluate(@case.Source),
            "clone-own" => EvaluateClone(@case),
            "universal-toolchain" => EvaluateUt(@case),
            _ => throw new ArgumentException($"Unknown treatment: {treatment}")
        };
        Console.WriteLine(JsonSerializer.Serialize(new Observation(@case.Id, value.ToString("0.################", CultureInfo.InvariantCulture), null)));
    }
    catch (PricingException exception)
    {
        Console.WriteLine(JsonSerializer.Serialize(new Observation(@case.Id, null, exception.Code)));
    }
}
return 0;

static decimal EvaluateClone(OracleCase @case) => @case.Id switch
{
    "discount" or "discount-invalid" => DiscountVariant.Evaluate(@case.Source),
    "surcharge" or "surcharge-invalid" => SurchargeVariant.Evaluate(@case.Source),
    "both" => CombinedVariant.Evaluate(@case.Source),
    "base" => CombinedVariant.Evaluate(@case.Source),
    _ => throw new ArgumentOutOfRangeException(nameof(@case.Id))
};

static decimal EvaluateUt(OracleCase @case)
{
    using var runtime = @case.Id switch
    {
        "discount" or "discount-invalid" => UtPricingRuntime.Create("pricing.discount"),
        "surcharge" or "surcharge-invalid" => UtPricingRuntime.Create("pricing.surcharge"),
        "both" => UtPricingRuntime.Create("pricing.discount", "pricing.surcharge"),
        "base" => UtPricingRuntime.Create(),
        _ => throw new ArgumentOutOfRangeException(nameof(@case.Id))
    };
    return runtime.Evaluate(@case.Source);
}

internal sealed record OracleCase(string Id, string Source, string? Expected, string? Error);
internal sealed record Observation(string Id, string? Value, string? Error);
