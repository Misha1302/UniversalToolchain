using BasicCore.ParserWrapper;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectRootAstNode : AstNode
{
    public DialectRootAstNode(string dialectName, IReadOnlyList<DialectDirectiveAstNode> directives)
        : base(DialectAstNodeTypes.DialectRoot, null, [])
    {
        if (string.IsNullOrWhiteSpace(dialectName))
            Thrower.Argument(nameof(dialectName), "Dialect name must not be empty.");

        DialectName = dialectName;
        if (directives == null)
            Thrower.ArgumentNull(nameof(directives));

        Directives = directives.ToList().AsReadOnly();
    }

    public string DialectName { get; }

    public IReadOnlyList<DialectDirectiveAstNode> Directives { get; }
}

public abstract class DialectDirectiveAstNode : AstNode
{
    protected DialectDirectiveAstNode(BasicTypesExtensions.ExtensibleEnum<AstNodeTag> nodeType) : base(nodeType, null, [])
    {
    }
}

public sealed class UseModulesDirectiveAstNode(IReadOnlyList<string> moduleNames) : DialectDirectiveAstNode(DialectAstNodeTypes.UseModulesDirective)
{
    public IReadOnlyList<string> ModuleNames { get; } = moduleNames;
}

public sealed class ExcludeModulesDirectiveAstNode(IReadOnlyList<string> moduleNames) : DialectDirectiveAstNode(DialectAstNodeTypes.ExcludeModulesDirective)
{
    public IReadOnlyList<string> ModuleNames { get; } = moduleNames;
}

public sealed class FrontendOrderDirectiveAstNode(IReadOnlyList<string> moduleNames) : DialectDirectiveAstNode(DialectAstNodeTypes.FrontendOrderDirective)
{
    public IReadOnlyList<string> ModuleNames { get; } = moduleNames;
}

public sealed class MiddleEndOrderDirectiveAstNode(IReadOnlyList<string> moduleNames) : DialectDirectiveAstNode(DialectAstNodeTypes.MiddleEndOrderDirective)
{
    public IReadOnlyList<string> ModuleNames { get; } = moduleNames;
}

public sealed class BackendOrderDirectiveAstNode(IReadOnlyList<string> moduleNames) : DialectDirectiveAstNode(DialectAstNodeTypes.BackendOrderDirective)
{
    public IReadOnlyList<string> ModuleNames { get; } = moduleNames;
}

public sealed class AllowedBackendDirectiveAstNode(DialectBackendTarget backend, bool enabled) : DialectDirectiveAstNode(DialectAstNodeTypes.AllowedBackendDirective)
{
    public DialectBackendTarget Backend { get; } = backend;
    public bool Enabled { get; } = enabled;
}

public sealed class RequiredIntrinsicDirectiveAstNode(string intrinsicName, bool allowed, DialectBackendTarget target) : DialectDirectiveAstNode(DialectAstNodeTypes.RequiredIntrinsicDirective)
{
    public string IntrinsicName { get; } = intrinsicName;
    public bool Allowed { get; } = allowed;
    public DialectBackendTarget Target { get; } = target;
}

public sealed class RequiredOptimizerDirectiveAstNode(string optimizerName, bool enabled, DialectBackendTarget target) : DialectDirectiveAstNode(DialectAstNodeTypes.RequiredOptimizerDirective)
{
    public string OptimizerName { get; } = optimizerName;
    public bool Enabled { get; } = enabled;
    public DialectBackendTarget Target { get; } = target;
}

public sealed class SecurityModeDirectiveAstNode(DialectSecurityProfile mode) : DialectDirectiveAstNode(DialectAstNodeTypes.SecurityModeDirective)
{
    public DialectSecurityProfile Mode { get; } = mode;
}

public sealed class CapabilityDirectiveAstNode(string capabilityName, bool value) : DialectDirectiveAstNode(DialectAstNodeTypes.CapabilityDirective)
{
    public string CapabilityName { get; } = capabilityName;
    public bool Value { get; } = value;
}
