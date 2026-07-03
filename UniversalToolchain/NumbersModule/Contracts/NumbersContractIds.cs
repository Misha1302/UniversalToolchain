using UniversalToolchain.ModuleContracts;

namespace NumbersModule.Contracts;

public static class NumbersContractIds
{
    public static ModuleId Module { get; } = new("wist.numbers");

    public static AstNodeKind NumberNode { get; } = new("wist.numbers.ast.number");

    public static BytecodePatternId PushRealNumber { get; } = new("wist.numbers.bytecode.push-real-number");
}
