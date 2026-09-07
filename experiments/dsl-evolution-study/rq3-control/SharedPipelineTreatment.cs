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
                "discount" => ApplyDiscount(program),
                "surcharge" => ApplySurcharge(program),
                _ => throw new ArgumentOutOfRangeException(nameof(features), feature, "Unknown feature")
            };
        }
        return new Rq3Result(program.Value, program.Operations.Count);
    }

    private static PricingProgram ApplyDiscount(PricingProgram program)
    {
        var value = program.Value;
        foreach (var operation in program.Operations.Where(static op => op.Name == "discount"))
            value *= 1m - operation.Percent / 100m;
        return program.WithValue(value);
    }

    private static PricingProgram ApplySurcharge(PricingProgram program)
    {
        var value = program.Value;
        foreach (var operation in program.Operations.Where(static op => op.Name == "surcharge"))
            value *= 1m + operation.Percent / 100m;
        return program.WithValue(value);
    }
}
