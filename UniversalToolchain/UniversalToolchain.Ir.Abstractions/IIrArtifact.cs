namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Represents one immutable or mutable IR artifact flowing through the generic pipeline.
/// </summary>
public interface IIrArtifact
{
    IrKind Kind { get; }
}
