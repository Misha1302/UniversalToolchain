using System.Globalization;

namespace DslEvolutionStudy;

internal sealed record PricingOperation(string Name, decimal Percent);

internal sealed record PricingProgram(decimal Value, IReadOnlyList<PricingOperation> Operations)
{
    public PricingProgram WithValue(decimal value) => this with { Value = value };
}

internal static class PricingParser
{
    public static PricingProgram Parse(string source)
    {
        var segments = source.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var head = segments[0].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (head.Length != 2 || head[0] != "price")
            throw new PricingException("SYNTAX", "Expected: price <decimal>.");

        var operations = new List<PricingOperation>();
        foreach (var segment in segments.Skip(1))
        {
            var parts = segment.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new PricingException("SYNTAX", "Expected: <operation> <percent>.");
            operations.Add(new PricingOperation(parts[0], ParseDecimal(parts[1])));
        }
        return new PricingProgram(ParseDecimal(head[1]), operations);
    }

    private static decimal ParseDecimal(string text) =>
        decimal.Parse(text, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
}

internal sealed class PricingException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
