using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core;

internal interface IDialectPolicyValidationRule
{
    void Validate(
        SecurityProfile? securityProfile,
        IReadOnlyDictionary<string, bool> capabilities,
        List<DialectDiagnostic> diagnostics);
}
