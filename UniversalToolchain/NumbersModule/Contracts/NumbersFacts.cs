using UniversalToolchain.ModuleContracts;

namespace NumbersModule.Contracts;

public static class NumbersFacts
{
    public static CompilerFactId NumericValuesSupported { get; } = new("wist.types.numeric-values-supported");
}
