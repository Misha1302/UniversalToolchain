namespace AbstractIrConverters;

public class AbstractIrToAbstractIrStub : IAbstractIrCompiler<IAbstractIR>
{
    public static IReadOnlyList<string> SupportedIntrinsicIds { get; } = Array.AsReadOnly(new[]
        {
            "call C#",
            "call C# ctor"
        }
        .Distinct(StringComparer.Ordinal)
        .OrderBy(static x => x, StringComparer.Ordinal)
        .ToArray());

    public IReadOnlyList<string> SupportedIntrinsics => SupportedIntrinsicIds;

    public IAbstractIR Compile(IAbstractIR air, CompilationInput input) => air;
}