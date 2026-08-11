namespace UniversalToolchain.LanguageSdk;

internal static class LanguagePlanningDiagnostics
{
    public static bool HasErrors(IEnumerable<LanguageDiagnostic> diagnostics) =>
        diagnostics.Any(static x => x.Severity == LanguageDiagnosticSeverity.Error);

    public static LanguageDiagnostic Error(string code, string stage, string message, string? owner, string hint) =>
        new(code, LanguageDiagnosticSeverity.Error, stage, message, owner, hint);
}
