namespace UniversalToolchain.Wist;

public abstract record WistDialectSource
{
    private WistDialectSource()
    {
    }

    public sealed record ShippedPreset(string PresetId) : WistDialectSource;

    public sealed record File(string Path) : WistDialectSource;

    public sealed record Text(string SourceText, string SourceName) : WistDialectSource;

    public static WistDialectSource FromShippedPreset(string presetId) =>
        new ShippedPreset(RequireText(presetId, nameof(presetId)));

    public static WistDialectSource FromFile(string path) =>
        new File(RequireText(path, nameof(path)));

    public static WistDialectSource FromText(string sourceText, string sourceName = "inline.wistdialect") =>
        new Text(
            sourceText ?? throw new ArgumentNullException(nameof(sourceText)),
            RequireText(sourceName, nameof(sourceName)));

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;
}
