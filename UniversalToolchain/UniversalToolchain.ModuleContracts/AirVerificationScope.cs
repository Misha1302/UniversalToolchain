namespace UniversalToolchain.ModuleContracts;

[Flags]
public enum AirVerificationScope
{
    None = 0,
    Structural = 1,
    Semantic = 2,
    Full = Structural | Semantic
}
