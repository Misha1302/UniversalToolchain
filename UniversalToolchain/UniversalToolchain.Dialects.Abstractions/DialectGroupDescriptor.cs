namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Describes a compile-time dialect group that expands into ordinary dialect directives.
/// </summary>
public sealed record DialectGroupDescriptor(
    string Alias,
    IReadOnlyList<string> IncludedModules,
    IReadOnlyList<KeyValuePair<string, bool>> Capabilities);