using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core;

internal static class DialectSecurityCapabilityPolicyValidator
{
    private static readonly IReadOnlyList<IDialectPolicyValidationRule> _defaultRules =
    [
        new RestrictedProfileUnsafeInteropRule()
    ];

    public static void Validate(
        SecurityProfile? securityProfile,
        IReadOnlyDictionary<string, bool> capabilities,
        List<DialectDiagnostic> diagnostics)
    {
        capabilities = capabilities.ArgNotNull();

        diagnostics = diagnostics.ArgNotNull();

        Validate(securityProfile, capabilities, diagnostics, _defaultRules);
    }

    public static void Validate(
        SecurityProfile? securityProfile,
        IReadOnlyDictionary<string, bool> capabilities,
        List<DialectDiagnostic> diagnostics,
        IReadOnlyList<IDialectPolicyValidationRule> rules)
    {
        capabilities = capabilities.ArgNotNull();

        diagnostics = diagnostics.ArgNotNull();

        rules = rules.ArgNotNull();

        foreach (var rule in rules)
            rule.Validate(securityProfile, capabilities, diagnostics);
    }
}