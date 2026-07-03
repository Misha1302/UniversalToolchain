namespace UniversalToolchain.ModuleContracts;

public sealed record CompatibilityAliasRecord(
    string LegacyId,
    ContractId Replacement,
    ContractSchemaVersion IntroducedIn,
    ContractSchemaVersion? DeprecatedIn);
