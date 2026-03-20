namespace UniversalToolchain.Dialects.Core;

internal sealed class RestrictedProfileUnsafeInteropRule : IDialectPolicyValidationRule
{
    public void Validate(
        SecurityProfile? securityProfile,
        IReadOnlyDictionary<string, bool> capabilities,
        List<DialectDiagnostic> diagnostics)
    {
        if (securityProfile != SecurityProfile.Restricted)
            return;

        if (capabilities.TryGetValue("unsafe-interop", out var enabled) && enabled)
            diagnostics.Add(new DialectDiagnostic(
                "S006",
                "Capability 'unsafe-interop' cannot be enabled under restricted security profile.",
                DialectDiagnosticSeverity.Error));
    }
}