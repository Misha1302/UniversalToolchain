using UniversalToolchain.ModuleContracts;

namespace VariablesModule.Contracts;

public static class VariablesContractIds
{
    public static ModuleId Module { get; } = new("wist.variables");

    public static AstNodeKind VariableNode { get; } = new("wist.variables.ast.variable");

    public static BytecodeTagId WriteTargetTypeInference { get; } = new("wist.variables.tag.write-target-type-inference");

    [Obsolete("Use WriteTargetTypeInference. The old name described a cross-module implementation detail, not the bytecode contract.")]
    public static BytecodeTagId ExpectingWriteTypeInference => WriteTargetTypeInference;

    public static BytecodePatternId LocalRead { get; } = new("wist.variables.bytecode.local-read");

    public static BytecodePatternId ExternalRead { get; } = new("wist.variables.bytecode.external-read");

    public static BytecodePatternId WriteTypeInference { get; } = new("wist.variables.bytecode.write-type-inference");

    public static BytecodePatternId DefineArgument { get; } = new("wist.variables.bytecode.define-argument");
}
