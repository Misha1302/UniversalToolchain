namespace UniversalToolchain.ModuleContracts;

internal static class VerificationSeveritySelector
{
    public static ToolchainDiagnosticSeverity Select(VerificationSeverityProfile profile) => profile switch
    {
        VerificationSeverityProfile.Observe => ToolchainDiagnosticSeverity.Info,
        VerificationSeverityProfile.Warn => ToolchainDiagnosticSeverity.Warning,
        VerificationSeverityProfile.EnforceNew => ToolchainDiagnosticSeverity.Error,
        VerificationSeverityProfile.EnforceSelected => ToolchainDiagnosticSeverity.Error,
        VerificationSeverityProfile.Strict => ToolchainDiagnosticSeverity.Error,
        _ => Thrower.InvalidOpEx<ToolchainDiagnosticSeverity>("Unknown verification severity profile.")
    };
}
