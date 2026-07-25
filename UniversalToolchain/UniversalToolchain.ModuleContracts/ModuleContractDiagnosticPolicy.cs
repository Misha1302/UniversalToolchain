namespace UniversalToolchain.ModuleContracts;

public sealed class ModuleContractDiagnosticPolicy(IModuleContractDiagnosticSink sink) : IModuleContractDiagnosticPolicy
{
    private readonly IModuleContractDiagnosticSink _sink = sink.ArgNotNull();

    public void ReportAndThrowIfErrors(
        string stage,
        IReadOnlyList<ToolchainDiagnostic> diagnostics)
    {
        if (diagnostics.Count > 0)
            _sink.Report(new ModuleContractPipelineDiagnosticBatch(stage, diagnostics));

        var errors = diagnostics
            .Where(static diagnostic => diagnostic.Severity == ToolchainDiagnosticSeverity.Error)
            .ToArray();
        if (errors.Length == 0)
            return;

        throw new ModuleContractVerificationException(stage, errors);
    }
}
