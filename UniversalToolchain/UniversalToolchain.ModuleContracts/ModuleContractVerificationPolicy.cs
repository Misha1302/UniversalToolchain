namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// Controls semantic verification scheduling while leaving the compiler plan and artifacts unchanged.
/// </summary>
public enum ModuleContractVerificationPolicy
{
    P0Structural,
    P1Invalidation,
    P1DemandRecomputation,
    P2Selective,
    P3Always
}
