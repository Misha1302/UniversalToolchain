using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectAstToBytecodeVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data == null)
            Thrower.ArgumentNull(nameof(data));

        var root = data.Node as DialectRootAstNode;
        if (root == null && data.Node.NodeType == DialectAstNodeTypes.Scope)
            root = data.Node.Children.SingleOrDefault() as DialectRootAstNode;

        if (root == null)
            return;

        data.Bytecode.Instructions.Add(new BytecodeInstruction(new DialectDirectiveConvertable(new DialectNameAirAnnotation(root.DialectName), "dialect_name")));

        foreach (var directive in root.Directives)
        {
            data.Bytecode.Instructions.Add(new BytecodeInstruction(new DialectDirectiveConvertable(ToAnnotation(directive), directive.NodeType.GetName())));
        }
    }

    private static IDialectAirAnnotation ToAnnotation(DialectDirectiveAstNode directive)
    {
        return directive switch
        {
            UseModulesDirectiveAstNode use => new UseModulesAirAnnotation(use.ModuleNames),
            ExcludeModulesDirectiveAstNode exclude => new ExcludeModulesAirAnnotation(exclude.ModuleNames),
            FrontendOrderDirectiveAstNode frontend => new FrontendOrderAirAnnotation(frontend.ModuleNames[0], frontend.ModuleNames[1]),
            MiddleEndOrderDirectiveAstNode middleEnd => new MiddleEndOrderAirAnnotation(middleEnd.ModuleNames[0], middleEnd.ModuleNames[1]),
            BackendOrderDirectiveAstNode backend => new BackendOrderAirAnnotation(backend.ModuleNames[0], backend.ModuleNames[1]),
            AllowedBackendDirectiveAstNode allowedBackend => new AllowedBackendsAirAnnotation(allowedBackend.Backend, allowedBackend.Enabled),
            RequiredIntrinsicDirectiveAstNode intrinsic => new RequiredIntrinsicsAirAnnotation(intrinsic.IntrinsicName, intrinsic.Allowed, intrinsic.Target),
            RequiredOptimizerDirectiveAstNode optimizer => new RequiredOptimizersAirAnnotation(optimizer.OptimizerName, optimizer.Enabled, optimizer.Target),
            SecurityModeDirectiveAstNode security => new SecurityModeAirAnnotation(security.Mode),
            CapabilityDirectiveAstNode capability => new CapabilitiesAirAnnotation(capability.CapabilityName, capability.Value),
            _ => Thrower.NotSupported<IDialectAirAnnotation>($"Unsupported dialect directive AST node: {directive.GetType().Name}")
        };
    }
}
