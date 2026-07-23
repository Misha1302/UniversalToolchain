namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Stores a typed canonical value for semantic comparison without relying on object ToString defaults.
/// </summary>
public sealed record PlanFuzzValueSnapshot(string TypeIdentity, string CanonicalValue)
{
    public static PlanFuzzValueSnapshot FromDecimal(decimal value) =>
        new(typeof(decimal).FullName.NotNull(), value.ToString("G29", CultureInfo.InvariantCulture));

    public static PlanFuzzValueSnapshot FromBoolean(bool value) =>
        new(typeof(bool).FullName.NotNull(), value ? "true" : "false");

    public static PlanFuzzValueSnapshot FromString(string value) =>
        new(typeof(string).FullName.NotNull(), value.ArgNotNull());
}
