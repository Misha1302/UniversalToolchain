namespace UniversalToolchain.ModuleContracts;

public static class KnownCoreIntrinsicSymbols
{
    public static IntrinsicSymbolId CallCSharp { get; } = new("call C#");

    public static IntrinsicSymbolId CallCSharpConstructor { get; } = new("call C# ctor");
}
