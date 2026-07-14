namespace UniversalToolchain.ModuleContracts;

public static class KnownCoreIntrinsicSymbols
{
    public static IntrinsicSymbolId CallCSharp { get; } = new(IntrinsicCapabilityIds.CallCSharp);

    public static IntrinsicSymbolId CallCSharpConstructor { get; } = new(IntrinsicCapabilityIds.CallCSharpConstructor);
}
