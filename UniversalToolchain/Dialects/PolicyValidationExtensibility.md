# Dialect policy validation extensibility seam

## What was removed

Previously, policy validation used a hardcoded branch in `DialectSecurityCapabilityPolicyValidator`:

- direct check for `SecurityProfile.Restricted`
- direct string check for capability `"unsafe-interop"`
- direct diagnostic emission in one centralized condition

## New shape

Validation now runs through a compact rule pipeline:

- `IDialectPolicyValidationRule` defines one rule contract.
- `DialectSecurityCapabilityPolicyValidator` executes an ordered rule list deterministically.
- `RestrictedProfileUnsafeInteropRule` preserves current default behavior (`S006`).

## Adding a new policy rule

1. Implement `IDialectPolicyValidationRule`.
2. Register it in the rule list used for validation (default list, or custom list in tests/composition).

Example (shape only):

```csharp
internal sealed class RequireCapabilityRule : IDialectPolicyValidationRule
{
    public void Validate(SecurityProfile? securityProfile, IReadOnlyDictionary<string, bool> capabilities, List<DialectDiagnostic> diagnostics)
    {
        if (!capabilities.ContainsKey("sandbox"))
        {
            diagnostics.Add(new DialectDiagnostic("S900", "Capability 'sandbox' must be explicitly declared.", DialectDiagnosticSeverity.Error));
        }
    }
}
```

This keeps extension local to rule classes and avoids growing central `if/switch` blocks.
