namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
/// Represents a dialect definition contract.
/// </summary>
public interface IDialectDefinition
{
    string Name { get; }

    string? Version { get; }

    string? BaseDialectName { get; }
}
