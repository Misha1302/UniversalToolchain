namespace DslEvolutionStudy;

internal static class ControlEvaluator
{
    public static decimal Evaluate(string source)
    {
        var program = PricingParser.Parse(source);
        var value = program.Value;
        foreach (var operation in program.Operations)
        {
            if (operation.Name != "discount")
                throw new PricingException("UNSUPPORTED", $"Unsupported operation: {operation.Name}.");
            var percent = ControlPercentRule.RequireValid(operation.Percent);
            value *= 1m - percent / 100m;
        }
        return value;
    }
}

internal static class ControlPercentRule
{
    public static decimal RequireValid(decimal percent)
    {
        if (percent is < 0 or > 100)
            throw new PricingException("PERCENT_RANGE", "Percent must be in [0, 100].");
        return percent;
    }
}
