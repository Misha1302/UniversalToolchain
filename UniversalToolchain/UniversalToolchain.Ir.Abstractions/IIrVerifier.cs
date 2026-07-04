namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Verifies a single IR kind at a pipeline boundary.
/// </summary>
public interface IIrVerifier
{
    IrKind Kind { get; }

    IrVerificationResult Verify(IIrArtifact artifact, IrPipelineContext context);
}
