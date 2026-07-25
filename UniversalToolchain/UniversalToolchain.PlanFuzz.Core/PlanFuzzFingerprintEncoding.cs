namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Produces unambiguous deterministic fingerprint material for attacker- or adapter-controlled strings.
/// </summary>
internal static class PlanFuzzFingerprintEncoding
{
    public static string EncodeAtom(string value)
    {
        value = value.ArgNotNull();
        return $"{Encoding.UTF8.GetByteCount(value).ToString(CultureInfo.InvariantCulture)}:{value}";
    }

    public static string EncodeSequence(IEnumerable<string> values)
    {
        values = values.ArgNotNull();
        return string.Concat(values.Select(EncodeAtom));
    }

    public static string EncodeFields(params string[] values) =>
        EncodeSequence(values.ArgNotNull());
}
