namespace UniversalToolchain.Diagnostics.Abstractions;

public static class ToolchainDiagnosticCodes
{
    public const string UnknownFunction = "UTC-FUNC-001";
    public const string FunctionUnavailable = "UTC-FUNC-002";
    public const string FunctionUnsupportedBackend = "UTC-FUNC-003";
    public const string WrongFunctionArgumentCount = "UTC-FUNC-004";
    public const string WrongFunctionArgumentType = "UTC-FUNC-005";

    public const string UnknownBinding = "UTC-BIND-001";
    public const string BindingConflict = "UTC-BIND-002";
    public const string TypeMismatch = "UTC-TYPE-001";

    public const string CapabilityProviderInvalid = "UTC-CAP-001";

    public const string RuleUnknown = "UTC-RULE-001";
    public const string RuleArgumentMissing = "UTC-RULE-002";
    public const string RuleArgumentUnknown = "UTC-RULE-003";
    public const string RuleArgumentNull = "UTC-RULE-004";
    public const string RuleArgumentTypeMismatch = "UTC-RULE-005";
    public const string RuleDuplicateName = "UTC-RULE-006";
    public const string RuleDuplicateParameter = "UTC-RULE-007";
    public const string RuleUnknownType = "UTC-RULE-008";
    public const string RuleInvalidBody = "UTC-RULE-009";
}
