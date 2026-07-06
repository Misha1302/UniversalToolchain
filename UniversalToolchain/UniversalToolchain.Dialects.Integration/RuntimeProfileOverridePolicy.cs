namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Controls how runtime profile defaults interact with explicit source directives.
/// </summary>
public enum RuntimeProfileOverridePolicy
{
    ExplicitSourceWins,
    StrictNoConflicts
}
