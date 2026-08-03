namespace UniversalToolchain.Wist;

/// <summary>
/// Controls semantic contract verification scheduling for internal experiments and tests.
/// </summary>
internal enum WistVerificationPolicy
{
    P0Structural,
    P1Invalidation,
    P1DemandRecomputation,
    P2Selective,
    P3Always
}
