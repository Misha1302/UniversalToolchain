using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Wist;

internal static class WistVerificationRuntimePackageFactory
{
    public static WistLanguageFeaturePackage Create(WistVerificationPolicy policy) =>
        new(Map(policy));

    private static WistCompilationVerificationPolicy Map(WistVerificationPolicy policy) => policy switch
    {
        WistVerificationPolicy.P0Structural => WistCompilationVerificationPolicy.P0Structural,
        WistVerificationPolicy.P1Invalidation => WistCompilationVerificationPolicy.P1Invalidation,
        WistVerificationPolicy.P2Selective => WistCompilationVerificationPolicy.P2Selective,
        WistVerificationPolicy.P3Always => WistCompilationVerificationPolicy.P3Always,
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown Wist verification policy.")
    };
}
