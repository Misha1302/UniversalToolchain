namespace UniversalToolchain.ModuleContracts;

public sealed record AirBackendPolicy
{
    public AirBackendPolicy(
        bool rejectNonUniversalIntrinsics,
        IEnumerable<IntrinsicSymbolId> universalIntrinsicAllowList)
    {
        universalIntrinsicAllowList = universalIntrinsicAllowList.ArgNotNull();

        RejectNonUniversalIntrinsics = rejectNonUniversalIntrinsics;
        UniversalIntrinsicAllowList = universalIntrinsicAllowList
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .Distinct()
            .ToArray();
    }

    public bool RejectNonUniversalIntrinsics { get; }

    public IReadOnlyList<IntrinsicSymbolId> UniversalIntrinsicAllowList { get; }

    public static AirBackendPolicy CapabilityGated { get; } = new(false, []);

    public static AirBackendPolicy UniversalInterpreter { get; } = new(
        true,
        [
            KnownCoreIntrinsicSymbols.CallCSharp,
            KnownCoreIntrinsicSymbols.CallCSharpConstructor
        ]);
}
