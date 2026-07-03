namespace UniversalToolchain.ModuleContracts;

public interface IModuleContractDiagnosticPolicy
{
    void ReportAndThrowIfErrors(
        string stage,
        IReadOnlyList<ToolchainDiagnostic> diagnostics);
}
