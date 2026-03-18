namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveAccumulation
{
    public List<string> UseModules { get; } = [];

    public List<string> ExcludeModules { get; } = [];

    public List<string> RequiresModules { get; } = [];

    public List<string> BeforeModules { get; } = [];

    public List<string> AfterModules { get; } = [];

    public List<string> Backends { get; } = [];

    public List<string> AllowedIntrinsics { get; } = [];

    public List<string> ForbiddenIntrinsics { get; } = [];

    public List<string> EnabledIntrinsics { get; } = [];

    public List<string> DisabledIntrinsics { get; } = [];

    public List<string> Capabilities { get; } = [];

    public DialectSecurityProfile? SecurityProfile { get; set; }
}
