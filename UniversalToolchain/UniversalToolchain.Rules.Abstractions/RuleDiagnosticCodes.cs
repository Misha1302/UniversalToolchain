namespace UniversalToolchain.Rules.Abstractions;

public static class RuleDiagnosticCodes
{
    public const string UnknownFunction = "WST-FUNC-001";
    public const string FunctionUnavailable = "WST-FUNC-002";
    public const string FunctionUnsupportedBackend = "WST-FUNC-003";
    public const string WrongFunctionArgumentCount = "WST-FUNC-004";
    public const string WrongFunctionArgumentType = "WST-FUNC-005";

    public const string DuplicateRuleName = "WST-RULE-001";
    public const string UnknownRuleType = "WST-RULE-002";
    public const string RuleReturnTypeMismatch = "WST-RULE-003";
    public const string DuplicateRuleParameterName = "WST-RULE-004";

    public const string UnknownBinding = "WST-BIND-001";
    public const string BindingNameConflict = "WST-BIND-002";
    public const string TypeMismatch = "WST-TYPE-001";
}
