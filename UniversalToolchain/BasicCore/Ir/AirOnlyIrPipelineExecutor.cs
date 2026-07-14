namespace BasicCore.Ir;

internal sealed class AirOnlyIrPipelineExecutor
{
    private readonly IReadOnlyList<IAirOptimizer> _optimizers;

    public AirOnlyIrPipelineExecutor(IReadOnlyList<IAirOptimizer> optimizers)
    {
        _optimizers = optimizers.ArgNotNull();
    }

    public IAbstractIR Optimize(IAbstractIR air)
    {
        air = air.ArgNotNull();

        IIrArtifact current = new AirArtifact(air);
        var context = new IrPipelineContext();
        foreach (var optimizer in _optimizers)
            current = optimizer.Run(current, context).Artifact;

        return current.As<AirArtifact>().Program;
    }
}
