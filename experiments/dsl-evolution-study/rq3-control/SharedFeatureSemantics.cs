using DslEvolutionStudy;

namespace DslEvolutionStudy.Rq3;

internal static class SharedFeatureSemantics
{
    public static PricingProgram ApplyDiscount(PricingProgram program)
    {
        var value = program.Value;
        foreach (var operation in program.Operations.Where(static op => op.Name == "discount"))
            value *= 1m - operation.Percent / 100m;
        return program.WithValue(value);
    }

    public static PricingProgram ApplySurcharge(PricingProgram program)
    {
        var value = program.Value;
        foreach (var operation in program.Operations.Where(static op => op.Name == "surcharge"))
            value *= 1m + operation.Percent / 100m;
        return program.WithValue(value);
    }
}
