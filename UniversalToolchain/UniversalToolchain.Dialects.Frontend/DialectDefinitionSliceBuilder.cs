using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSliceBuilder
{
    private string? _name;
    private readonly List<string> _useModules = [];
    private readonly List<string> _excludeModules = [];
    private readonly List<DialectOrderDirective> _orderDirectives = [];
    private readonly List<DialectBackendDirective> _backendDirectives = [];
    private readonly List<DialectIntrinsicDirective> _intrinsicDirectives = [];
    private readonly List<DialectOptimizerDirective> _optimizerDirectives = [];
    private readonly List<DialectCapabilityDirective> _capabilityDirectives = [];
    private DialectSecurityProfile? _securityProfile;

    public void Apply(IDialectAirAnnotation annotation)
    {
        switch (annotation)
        {
            case DialectNameAirAnnotation name:
                _name = name.Name;
                break;
            case UseModulesAirAnnotation use:
                _useModules.AddRange(use.ModuleNames);
                break;
            case ExcludeModulesAirAnnotation exclude:
                _excludeModules.AddRange(exclude.ModuleNames);
                break;
            case FrontendOrderAirAnnotation order:
                _orderDirectives.Add(new DialectOrderDirective(DialectOrderDirectiveKind.Requires, order.SourceModule, order.TargetModule));
                break;
            case MiddleEndOrderAirAnnotation order:
                _orderDirectives.Add(new DialectOrderDirective(DialectOrderDirectiveKind.Before, order.SourceModule, order.TargetModule));
                break;
            case BackendOrderAirAnnotation order:
                _orderDirectives.Add(new DialectOrderDirective(DialectOrderDirectiveKind.After, order.SourceModule, order.TargetModule));
                break;
            case AllowedBackendsAirAnnotation backend:
                _backendDirectives.Add(new DialectBackendDirective(backend.Backend, backend.Enabled));
                break;
            case RequiredIntrinsicsAirAnnotation intrinsic:
                _intrinsicDirectives.Add(new DialectIntrinsicDirective(intrinsic.Name, intrinsic.Allowed, intrinsic.Target));
                break;
            case RequiredOptimizersAirAnnotation optimizer:
                _optimizerDirectives.Add(new DialectOptimizerDirective(optimizer.Name, optimizer.Enabled, optimizer.Target));
                break;
            case SecurityModeAirAnnotation security:
                _securityProfile = security.SecurityProfile;
                break;
            case CapabilitiesAirAnnotation capability:
                _capabilityDirectives.Add(new DialectCapabilityDirective(capability.Name, capability.Value));
                break;
        }
    }

    public DialectDefinitionSlice Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
            throw new InvalidOperationException("Dialect AIR did not contain a dialect name annotation.");

        return new DialectDefinitionSlice(
            _name,
            _useModules,
            _excludeModules,
            _orderDirectives,
            _backendDirectives,
            _intrinsicDirectives,
            _optimizerDirectives,
            _securityProfile,
            _capabilityDirectives);
    }
}
