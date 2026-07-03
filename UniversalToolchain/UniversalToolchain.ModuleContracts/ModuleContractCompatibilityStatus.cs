namespace UniversalToolchain.ModuleContracts;

public enum ModuleContractCompatibilityStatus
{
    LegacyImplicit,
    PartiallyDeclared,
    Declared,
    Verified,
    Enforced
}
