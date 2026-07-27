using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist.Presets;

/// <summary>
/// Describes one executable Wist dialect profile shipped with the runtime.
/// Backend metadata is explicit so facade construction can reject impossible
/// combinations before the first operation.
/// </summary>
public sealed record WistShippedDialectPreset
{
    public WistShippedDialectPreset(
        string id,
        string relativeDialectFilePath,
        string displayName,
        string description,
        string defaultBackend,
        IEnumerable<string> supportedBackends)
    {
        if (string.IsNullOrWhiteSpace(id))
            Thrower.Argument(nameof(id), "Preset id must not be empty.");
        if (string.IsNullOrWhiteSpace(relativeDialectFilePath))
            Thrower.Argument(nameof(relativeDialectFilePath), "Preset dialect path must not be empty.");
        if (string.IsNullOrWhiteSpace(defaultBackend))
            Thrower.Argument(nameof(defaultBackend), "Preset default backend must not be empty.");

        var backends = supportedBackends.ArgNotNull()
            .Select(static backend => backend?.Trim())
            .Where(static backend => !string.IsNullOrWhiteSpace(backend))
            .Select(static backend => backend!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static backend => backend, StringComparer.Ordinal)
            .ToArray();
        if (backends.Length == 0)
            Thrower.Argument(nameof(supportedBackends), "Preset must support at least one backend.");
        if (!backends.Contains(defaultBackend, StringComparer.Ordinal))
            Thrower.Argument(nameof(defaultBackend), $"Default backend '{defaultBackend}' is not in the supported backend set.");

        Id = id;
        RelativeDialectFilePath = relativeDialectFilePath;
        DisplayName = displayName;
        Description = description;
        DefaultBackend = defaultBackend;
        SupportedBackends = new ReadOnlyCollection<string>(backends);
    }

    public string Id { get; }
    public string RelativeDialectFilePath { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string DefaultBackend { get; }
    public IReadOnlyList<string> SupportedBackends { get; }
}
