namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Represents the artifact and fact state produced by one pipeline stage.
/// </summary>
public sealed class IrStageResult
{
    public IrStageResult(IIrArtifact artifact)
        : this(artifact, IrFactSet.Empty)
    {
    }

    public IrStageResult(IIrArtifact artifact, IrFactSet facts)
    {
        Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
        Facts = facts ?? throw new ArgumentNullException(nameof(facts));
    }

    public IIrArtifact Artifact { get; }

    public IrFactSet Facts { get; }
}
