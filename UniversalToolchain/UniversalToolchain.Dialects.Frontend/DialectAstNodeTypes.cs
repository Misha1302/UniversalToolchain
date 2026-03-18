using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectAstNodeTypes
{
    public static readonly AstNodeType Scope = AstNodeType.CreateOrGet("Scope");
    public static readonly AstNodeType DialectRoot = AstNodeType.CreateOrGet("DialectRoot");
    public static readonly AstNodeType UseModulesDirective = AstNodeType.CreateOrGet("UseModulesDirective");
    public static readonly AstNodeType ExcludeModulesDirective = AstNodeType.CreateOrGet("ExcludeModulesDirective");
    public static readonly AstNodeType FrontendOrderDirective = AstNodeType.CreateOrGet("FrontendOrderDirective");
    public static readonly AstNodeType MiddleEndOrderDirective = AstNodeType.CreateOrGet("MiddleEndOrderDirective");
    public static readonly AstNodeType BackendOrderDirective = AstNodeType.CreateOrGet("BackendOrderDirective");
    public static readonly AstNodeType AllowedBackendDirective = AstNodeType.CreateOrGet("AllowedBackendDirective");
    public static readonly AstNodeType RequiredIntrinsicDirective = AstNodeType.CreateOrGet("RequiredIntrinsicDirective");
    public static readonly AstNodeType RequiredOptimizerDirective = AstNodeType.CreateOrGet("RequiredOptimizerDirective");
    public static readonly AstNodeType SecurityModeDirective = AstNodeType.CreateOrGet("SecurityModeDirective");
    public static readonly AstNodeType CapabilityDirective = AstNodeType.CreateOrGet("CapabilityDirective");
}
