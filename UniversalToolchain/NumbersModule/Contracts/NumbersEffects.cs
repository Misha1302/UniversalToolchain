using UniversalToolchain.ModuleContracts;

namespace NumbersModule.Contracts;

public static class NumbersEffects
{
    public static CompilerEffectId LowerNumericLiteral { get; } = new("wist.numbers.effect.lower-numeric-literal");
}
