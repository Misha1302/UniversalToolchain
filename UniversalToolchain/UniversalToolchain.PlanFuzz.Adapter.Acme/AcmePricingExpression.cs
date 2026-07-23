namespace UniversalToolchain.PlanFuzz.Adapter.Acme;

internal sealed record AcmePricingExpression(decimal UnitPrice, decimal Quantity, decimal Discount)
{
    public static AcmePricingExpression Parse(string source)
    {
        source = source.ArgNotNull();
        var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 5 || parts[1] != "*" || parts[3] != "-")
            return Thrower.InvalidOpEx<AcmePricingExpression>("Expected '<unit-price> * <quantity> - <discount>'.");
        return new AcmePricingExpression(ParseDecimal(parts[0]), ParseDecimal(parts[2]), ParseDecimal(parts[4]));
    }

    public decimal Evaluate() => UnitPrice * Quantity - Discount;
    public decimal EvaluateWrongArithmetic() => UnitPrice * Quantity + Discount;
    public Func<decimal> Compile() => Evaluate;
    public Func<decimal> CompileWrongArithmetic() => EvaluateWrongArithmetic;

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
}
