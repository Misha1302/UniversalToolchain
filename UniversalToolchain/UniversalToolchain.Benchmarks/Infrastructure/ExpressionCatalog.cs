namespace UniversalToolchain.Benchmarks.Infrastructure;

public static class ExpressionCatalog
{
    public const string IntUnary = "x * 2 + 3";
    public const string IntBinary = "x * y + 7";
    public const string IntQuad = "a * b + c * d - a + 3";

    public const string DoublePricing = "price * 0.9 + fee";
    public const string DecimalPricing = "price * 0.9m + fee";

    public const string TernaryClamp = "x > 10 ? x : 0";
    public const string ShortCircuit = "(a > 0 && b > 0) || c > 0";
}
