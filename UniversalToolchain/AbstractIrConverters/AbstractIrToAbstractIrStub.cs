using BasicCore.Capabilities;
namespace AbstractIrConverters;

public class AbstractIrToAbstractIrStub : IAbstractIrCompiler<IAbstractIR>
{
    public static IReadOnlyList<string> SupportedIntrinsicIds { get; } = Array.AsReadOnly(new[]
        {
            IntrinsicCapabilityIds.CallCSharp,
            IntrinsicCapabilityIds.CallCSharpConstructor
        }
        .Distinct(StringComparer.Ordinal)
        .OrderBy(static x => x, StringComparer.Ordinal)
        .ToArray());

    public IReadOnlyList<string> SupportedIntrinsics => SupportedIntrinsicIds;

    public IAbstractIR Compile(IAbstractIR air, CompilationInput input) => air;
}