namespace BasicCore.Ir;

internal sealed class LegacyAirOptimizerStage<TCompilationOutput> : IIrOptimizationPass
{
    private readonly IAbstractIrCompiler<TCompilationOutput> _compiler;
    private readonly IIRProcessingModule _optimizer;

    public LegacyAirOptimizerStage(
        IIRProcessingModule optimizer,
        IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        _optimizer = optimizer.ArgNotNull();
        _compiler = compiler.ArgNotNull();
        Id = new IrStageId($"legacy-air-optimizer:{_optimizer.GetType().FullName}");
    }

    public IrStageId Id { get; }

    public IrKind InputKind => AirIrKinds.Air;

    public IrKind OutputKind => AirIrKinds.Air;

    public IrStageContract Contract => IrStageContract.Empty;

    public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
    {
        input = input.ArgNotNull();
        _ = context.ArgNotNull();

        var air = input.As<AirArtifact>().Program;
        var optimizedAir = _optimizer.ProcessIr(air, _compiler).ArgNotNull();
        return new IrStageResult(new AirArtifact(optimizedAir));
    }
}
