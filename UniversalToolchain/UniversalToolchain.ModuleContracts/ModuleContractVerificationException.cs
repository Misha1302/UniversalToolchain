namespace UniversalToolchain.ModuleContracts;

public sealed class ModuleContractVerificationException : InvalidOperationException
{
    public ModuleContractVerificationException(string stage, IReadOnlyList<ToolchainDiagnostic> diagnostics)
        : base($"Module contract {stage} failed: {string.Join("; ", diagnostics.Select(FormatDiagnostic))}")
    {
        Stage = stage;
        Diagnostics = diagnostics.ToArray();
    }

    public string Stage { get; }
    public IReadOnlyList<ToolchainDiagnostic> Diagnostics { get; }

    private static string FormatDiagnostic(ToolchainDiagnostic diagnostic) =>
        $"{diagnostic.Code}: {diagnostic.Message}";
}
