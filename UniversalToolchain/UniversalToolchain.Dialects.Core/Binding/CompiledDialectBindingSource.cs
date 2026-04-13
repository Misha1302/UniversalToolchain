using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Core.Binding;

internal sealed class CompiledDialectBindingSource : IDialectBindingSource
{
    private readonly ReadOnlyCollection<BackendBindingDirectiveRecord> _backendDirectives;
    private readonly ReadOnlyCollection<KeyValuePair<string, bool>> _capabilities;
    private readonly DialectDefinitionSlice _slice;
    private readonly ReadOnlyCollection<IntrinsicBindingDirectiveRecord> _intrinsicDirectives;
    private readonly ReadOnlyCollection<OptimizerBindingDirectiveRecord> _optimizerDirectives;
    private readonly ReadOnlyCollection<OrderBindingDirectiveRecord> _orderRules;

    public CompiledDialectBindingSource(DialectDefinitionSlice slice)
    {
        if (slice == null)
            Thrower.ArgumentNull(nameof(slice));

        _slice = slice;
        _orderRules = new ReadOnlyCollection<OrderBindingDirectiveRecord>(slice.OrderDirectives.Select(ToOrderRule).ToList());
        _backendDirectives = new ReadOnlyCollection<BackendBindingDirectiveRecord>(slice.BackendDirectives.Select(ToBackendDirective).ToList());
        _intrinsicDirectives = new ReadOnlyCollection<IntrinsicBindingDirectiveRecord>(slice.IntrinsicDirectives.Select(ToIntrinsicDirective).ToList());
        _optimizerDirectives = new ReadOnlyCollection<OptimizerBindingDirectiveRecord>(slice.OptimizerDirectives.Select(ToOptimizerDirective).ToList());
        _capabilities = new ReadOnlyCollection<KeyValuePair<string, bool>>(slice.CapabilityDirectives.Select(ToCapability).ToList());
    }

    public DialectBindingInputKind InputKind => DialectBindingInputKind.Compiled;

    public string Name => _slice.Name;

    public string? Version => _slice.Version;

    public string? BaseDialectName => _slice.BaseDialectName;

    public IReadOnlyList<string> UseModules => _slice.UseModules;

    public IReadOnlyList<string> ExcludeModules => _slice.ExcludeModules;

    public IReadOnlyList<OrderBindingDirectiveRecord> OrderRules => _orderRules;

    public IReadOnlyList<BackendBindingDirectiveRecord> BackendDirectives => _backendDirectives;

    public IReadOnlyList<IntrinsicBindingDirectiveRecord> IntrinsicDirectives => _intrinsicDirectives;

    public IReadOnlyList<OptimizerBindingDirectiveRecord> OptimizerDirectives => _optimizerDirectives;

    public SecurityProfile? SecurityProfile => _slice.SecurityProfile.HasValue ? ToSecurityProfile(_slice.SecurityProfile.Value) : null;

    public IReadOnlyList<KeyValuePair<string, bool>> Capabilities => _capabilities;

    private static OrderBindingDirectiveRecord ToOrderRule(DialectOrderDirective directive) => new(ToOrderRuleKind(directive.Kind), directive.SourceModule, directive.TargetModule);

    private static BackendBindingDirectiveRecord ToBackendDirective(DialectBackendDirective directive) => new(directive.Backend, directive.Enabled);

    private static IntrinsicBindingDirectiveRecord ToIntrinsicDirective(DialectIntrinsicDirective directive) => new(directive.Name, directive.Target, directive.Allowed);

    private static OptimizerBindingDirectiveRecord ToOptimizerDirective(DialectOptimizerDirective directive) => new(directive.Name, directive.Target, directive.Enabled);

    private static KeyValuePair<string, bool> ToCapability(DialectCapabilityDirective directive) => new(directive.Name, directive.Value);

    private static OrderRuleKind ToOrderRuleKind(DialectOrderDirectiveKind kind)
    {
        return kind switch
        {
            DialectOrderDirectiveKind.Before => OrderRuleKind.Before,
            DialectOrderDirectiveKind.After => OrderRuleKind.After,
            _ => OrderRuleKind.Requires
        };
    }

    private static SecurityProfile ToSecurityProfile(DialectSecurityProfile profile)
    {
        return profile switch
        {
            DialectSecurityProfile.Trusted => UniversalToolchain.Dialects.Abstractions.SecurityProfile.Trusted,
            _ => UniversalToolchain.Dialects.Abstractions.SecurityProfile.Restricted
        };
    }
}
