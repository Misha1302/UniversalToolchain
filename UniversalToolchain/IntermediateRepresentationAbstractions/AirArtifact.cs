using UniversalToolchain.Ir.Abstractions;

namespace IntermediateRepresentationAbstractions;

/// <summary>
/// Wraps the existing AIR boundary as a generic IR artifact without changing AIR semantics.
/// </summary>
public sealed class AirArtifact : IIrArtifact
{
    public AirArtifact(IAbstractIR program)
    {
        Program = program ?? throw new ArgumentNullException(nameof(program));
    }

    public IrKind Kind => AirIrKinds.Air;

    public IAbstractIR Program { get; }
}
