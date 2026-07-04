namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Common contract for deterministic IR stages.
/// </summary>
public interface IIrPipelineStage
{
    IrStageId Id { get; }

    IrKind InputKind { get; }

    IrKind OutputKind { get; }

    IrStageContract Contract { get; }

    IrStageResult Run(IIrArtifact input, IrPipelineContext context);
}
