namespace UniversalToolchain.Dialects.Wist.Presets;

/// <summary>
///     Describes a shipped Wist dialect file without duplicating its dialect contents.
/// </summary>
public sealed record WistShippedDialectPreset(
    string Id,
    string RelativeDialectFilePath,
    string DisplayName,
    string Description);
