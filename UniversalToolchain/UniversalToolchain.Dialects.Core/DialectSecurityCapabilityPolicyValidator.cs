using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Core;

internal static class DialectSecurityCapabilityPolicyValidator
{
    private static readonly IReadOnlyList<IDialectPolicyValidationRule> DefaultRules =
    [
        new RestrictedProfileUnsafeInteropRule()
    ];

    public static void Validate(
        SecurityProfile? securityProfile,
        IReadOnlyDictionary<string, bool> capabilities,
        List<DialectDiagnostic> diagnostics)
    {
        if (capabilities == null)
            Thrower.ArgumentNull(nameof(capabilities));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        Validate(securityProfile, capabilities, diagnostics, DefaultRules);
    }

    public static void Validate(
        SecurityProfile? securityProfile,
        IReadOnlyDictionary<string, bool> capabilities,
        List<DialectDiagnostic> diagnostics,
        IReadOnlyList<IDialectPolicyValidationRule> rules)
    {
        if (capabilities == null)
            Thrower.ArgumentNull(nameof(capabilities));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        if (rules == null)
            Thrower.ArgumentNull(nameof(rules));

        foreach (var rule in rules)
        {
            if (rule == null)
                Thrower.Argument(nameof(rules), "Policy rule list must not contain null entries.");

            rule.Validate(securityProfile, capabilities, diagnostics);
        }
    }
}
