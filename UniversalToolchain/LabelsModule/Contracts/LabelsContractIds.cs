using UniversalToolchain.ModuleContracts;

namespace LabelsModule.Contracts;

public static class LabelsContractIds
{
    public static ModuleId Module { get; } = new("wist.labels");

    public static AstNodeKind LabelNode { get; } = new("wist.labels.ast.label");

    public static AstNodeKind GotoNode { get; } = new("wist.labels.ast.goto");

    public static BytecodePatternId Label { get; } = new("wist.labels.bytecode.label");

    public static BytecodePatternId Goto { get; } = new("wist.labels.bytecode.goto");
}
