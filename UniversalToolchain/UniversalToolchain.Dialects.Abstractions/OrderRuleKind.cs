namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
/// Specifies how one module relates to another module for deterministic ordering.
/// </summary>
public enum OrderRuleKind
{
    Requires = 0,
    Before = 1,
    After = 2
}
