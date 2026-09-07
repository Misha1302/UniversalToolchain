using DslEvolutionStudy;

namespace DslEvolutionStudy.Rq3;

internal static class SharedPipelineTreatment
{
    public static Rq3Result Evaluate(string source, IReadOnlyList<string> features)
    {
        var program = PricingParser.Parse(source);
        foreach (var feature in features)
        {
            program = feature switch
            {
                "discount" => SharedFeatureSemantics.ApplyDiscount(program),
                "surcharge" => SharedFeatureSemantics.ApplySurcharge(program),
                _ => throw new ArgumentOutOfRangeException(nameof(features), feature, "Unknown feature")
            };
        }
        return new Rq3Result(program.Value, program.Operations.Count);
    }

}
