namespace UniversalToolchain.ModuleContracts;

public static class KnownCoreAstNodeKinds
{
    public static AstNodeKind Program { get; } = new("core.ast.program");

    public static AstNodeKind Unknown { get; } = new("core.ast.unknown");
}
