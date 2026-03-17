namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveAccumulation
{
    public List<string> UseModules { get; } = [];

    public List<string> ExcludeModules { get; } = [];

    public List<DialectOrderDirective> OrderDirectives { get; } = [];

    public List<DialectBackendDirective> BackendDirectives { get; } = [];

    public List<DialectIntrinsicDirective> IntrinsicDirectives { get; } = [];

    public List<DialectOptimizerDirective> OptimizerDirectives { get; } = [];

    public List<DialectCapabilityDirective> CapabilityDirectives { get; } = [];

    public DialectSecurityProfile? SecurityProfile { get; set; }
}
