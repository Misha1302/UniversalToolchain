namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Describes one language adapter and its testcase compatibility boundary.
/// </summary>
public sealed class PlanFuzzAdapterDescriptor
{
    public PlanFuzzAdapterDescriptor(
        string adapterId,
        string adapterVersion,
        string languageId,
        string generatorSchemaVersion,
        IEnumerable<string>? capabilities = null)
    {
        AdapterId = RequireText(adapterId, nameof(adapterId));
        AdapterVersion = RequireText(adapterVersion, nameof(adapterVersion));
        LanguageId = RequireText(languageId, nameof(languageId));
        GeneratorSchemaVersion = RequireText(generatorSchemaVersion, nameof(generatorSchemaVersion));
        Capabilities = new ReadOnlyCollection<string>((capabilities ?? [])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray());
    }

    public string AdapterId { get; }
    public string AdapterVersion { get; }
    public string LanguageId { get; }
    public string GeneratorSchemaVersion { get; }
    public IReadOnlyList<string> Capabilities { get; }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Thrower.Argument<string>(parameterName, $"Argument '{parameterName}' must not be empty.");
        return value;
    }
}
