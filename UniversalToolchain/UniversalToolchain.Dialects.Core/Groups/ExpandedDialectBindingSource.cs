using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;

namespace UniversalToolchain.Dialects.Core.Groups;

internal sealed class ExpandedDialectBindingSource : IDialectBindingSource
{
    private readonly IDialectBindingSource _inner;
    private readonly IReadOnlyList<KeyValuePair<string, bool>> _capabilities;
    private readonly IReadOnlyList<string> _useModules;

    public ExpandedDialectBindingSource(
        IDialectBindingSource inner,
        IReadOnlyList<string> useModules,
        IReadOnlyList<KeyValuePair<string, bool>> capabilities)
    {
        _inner = inner.ArgNotNull();
        _useModules = useModules.ArgNotNull();
        _capabilities = capabilities.ArgNotNull();
    }

    public DialectBindingInputKind InputKind => _inner.InputKind;

    public string Name => _inner.Name;

    public string? Version => _inner.Version;

    public string? BaseDialectName => _inner.BaseDialectName;

    public IReadOnlyList<string> UseModules => _useModules;

    public IReadOnlyList<string> ExcludeModules => _inner.ExcludeModules;

    public IReadOnlyList<OrderBindingDirectiveRecord> OrderRules => _inner.OrderRules;

    public IReadOnlyList<BackendBindingDirectiveRecord> BackendDirectives => _inner.BackendDirectives;

    public IReadOnlyList<IntrinsicBindingDirectiveRecord> IntrinsicDirectives => _inner.IntrinsicDirectives;

    public IReadOnlyList<OptimizerBindingDirectiveRecord> OptimizerDirectives => _inner.OptimizerDirectives;

    public SecurityProfile? SecurityProfile => _inner.SecurityProfile;

    public IReadOnlyList<KeyValuePair<string, bool>> Capabilities => _capabilities;
}
