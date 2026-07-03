namespace UniversalToolchain.ModuleContracts;

public interface IModuleContractDiagnosticSink
{
    void Report(ModuleContractPipelineDiagnosticBatch batch);
}

public sealed record ModuleContractPipelineDiagnosticBatch(
    string Stage,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);

public sealed class NullModuleContractDiagnosticSink : IModuleContractDiagnosticSink
{
    public static NullModuleContractDiagnosticSink Instance { get; } = new();

    private NullModuleContractDiagnosticSink()
    {
    }

    public void Report(ModuleContractPipelineDiagnosticBatch batch)
    {
        _ = batch.ArgNotNull();
    }
}
