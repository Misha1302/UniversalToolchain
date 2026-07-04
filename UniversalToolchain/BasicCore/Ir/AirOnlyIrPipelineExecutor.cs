namespace BasicCore.Ir;

internal sealed class AirOnlyIrPipelineExecutor<TCompilationOutput>
{
    private readonly IAbstractIrCompiler<TCompilationOutput> _compiler;
    private readonly IReadOnlyList<IIRProcessingModule> _optimizers;

    public AirOnlyIrPipelineExecutor(
        IReadOnlyList<IIRProcessingModule> optimizers,
        IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        _optimizers = optimizers.ArgNotNull();
        _compiler = compiler.ArgNotNull();
    }

    public IAbstractIR Optimize(IAbstractIR air)
    {
        air = air.ArgNotNull();

        IIrArtifact current = new AirArtifact(air);
        var context = new IrPipelineContext();
        foreach (var optimizer in _optimizers)
        {
            var stage = new LegacyAirOptimizerStage<TCompilationOutput>(optimizer, _compiler);
            current = stage.Run(current, context).Artifact;
        }

        return current.As<AirArtifact>().Program;
    }
}
