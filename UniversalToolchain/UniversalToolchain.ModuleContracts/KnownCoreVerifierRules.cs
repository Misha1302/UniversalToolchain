namespace UniversalToolchain.ModuleContracts;

public static class KnownCoreVerifierRules
{
    public static VerifierRuleId BytecodeContract { get; } = new("core.verifier.bytecode-contract");

    public static VerifierRuleId AirContract { get; } = new("core.verifier.air-contract");

    public static VerifierRuleId BackendInputContract { get; } = new("core.verifier.backend-input-contract");
}
