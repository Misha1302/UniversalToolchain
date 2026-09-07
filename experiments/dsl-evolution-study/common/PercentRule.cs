namespace DslEvolutionStudy;

internal static class PercentRule
{
    public static decimal RequireValid(decimal percent)
    {
        if (percent is < 0 or > 100)
            throw new PricingException("PERCENT_RANGE", "Percent must be in [0, 100].");
        return percent;
    }
}
