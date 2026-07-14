using BasicCore.Capabilities;

namespace BasicCore.Contracts;

/// <summary>
/// Represents a deterministic AIR optimization stage.
/// </summary>
public interface IAirOptimizer : IIrOptimizationPass
{
    /// <summary>
    /// Rewrites the current AIR program. Implementations must not return <see langword="null"/>.
    /// </summary>
    IAbstractIR Optimize(IAbstractIR current) => current;

    void InitIntrinsicCapabilityContext(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
    }

    void InitMethodsTranslator(IAbstractMethodsTranslator methodsTranslator)
    {
    }

    IrStageId IIrPipelineStage.Id =>
        new($"air-optimizer:{this.GetType().FullName ?? this.GetType().Name}");

    IrKind IIrPipelineStage.InputKind => AirIrKinds.Air;

    IrKind IIrPipelineStage.OutputKind => AirIrKinds.Air;

    IrStageContract IIrPipelineStage.Contract => AirOptimizerStageContract.Value;

    IrStageResult IIrPipelineStage.Run(IIrArtifact input, IrPipelineContext context)
    {
        input = input.ArgNotNull();
        _ = context.ArgNotNull();

        var air = input.As<AirArtifact>().Program;
        var optimizedAir = Optimize(air).ArgNotNull();
        return new IrStageResult(new AirArtifact(optimizedAir));
    }
}

internal static class AirOptimizerStageContract
{
    public static IrStageContract Value { get; } = new(
        invalidatesFacts:
        [
            new FactId("air.cfg"),
            new FactId("air.structural-verification")
        ]);
}
