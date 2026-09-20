namespace DslEvolutionStudy;

internal static class DiscountVariant
{
    public static decimal Evaluate(string source)
    {
        var program = PricingParser.Parse(source);
        var value = program.Value;
        foreach (var operation in program.Operations)
        {
            if (operation.Name != "discount")
                throw new PricingException("UNSUPPORTED", $"Unsupported operation: {operation.Name}.");
            var percent = ClonePercentRule.RequireValid(operation.Percent);
            value *= 1m - percent / 100m;
        }
        return value;
    }
}

internal static class SurchargeVariant
{
    public static decimal Evaluate(string source)
    {
        var program = PricingParser.Parse(source);
        var value = program.Value;
        foreach (var operation in program.Operations)
        {
            if (operation.Name != "surcharge")
                throw new PricingException("UNSUPPORTED", $"Unsupported operation: {operation.Name}.");
            var percent = ClonePercentRule.RequireValid(operation.Percent);
            value *= 1m + percent / 100m;
        }
        return value;
    }
}

internal static class CombinedVariant
{
    public static decimal Evaluate(string source)
    {
        var program = PricingParser.Parse(source);
        var value = program.Value;
        foreach (var operation in program.Operations)
        {
            var percent = ClonePercentRule.RequireValid(operation.Percent);
            value = operation.Name switch
            {
                "discount" => value * (1m - percent / 100m),
                "surcharge" => value * (1m + percent / 100m),
                _ => throw new PricingException("UNSUPPORTED", $"Unsupported operation: {operation.Name}.")
            };
        }
        return value;
    }
}

internal static class ClonePercentRule
{
    public static decimal RequireValid(decimal percent)
    {
        if (percent is < 0 or > 100)
            throw new PricingException("PERCENT_RANGE", "Percent must be in [0, 100].");
        return percent;
    }
}
