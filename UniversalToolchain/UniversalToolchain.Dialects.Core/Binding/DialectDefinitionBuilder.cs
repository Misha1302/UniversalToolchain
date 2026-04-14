using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding;

internal sealed class DialectDefinitionBuilder
{
    private string? _baseDialectName;
    private BackendPolicy? _backendPolicy;
    private CapabilityPolicy? _capabilityPolicy;
    private readonly Dictionary<string, object> _extensions = new(StringComparer.Ordinal);
    private IntrinsicPolicy? _intrinsicPolicy;
    private ModulePolicy? _modulePolicy;
    private string? _name;
    private OptimizerPolicy? _optimizerPolicy;
    private IReadOnlyList<OrderRule>? _orderRules;
    private SecurityPolicy? _securityPolicy;
    private bool _securityPolicySet;
    private string? _version;

    public void SetIdentity(string name, string? version, string? baseDialectName)
    {
        _name = name;
        _version = version;
        _baseDialectName = baseDialectName;
    }

    public void SetModulePolicy(ModulePolicy modulePolicy)
    {
        modulePolicy = modulePolicy.ArgNotNull();

        _modulePolicy = modulePolicy;
    }

    public void SetBackendPolicy(BackendPolicy backendPolicy)
    {
        backendPolicy = backendPolicy.ArgNotNull();

        _backendPolicy = backendPolicy;
    }

    public void SetIntrinsicPolicy(IntrinsicPolicy intrinsicPolicy)
    {
        intrinsicPolicy = intrinsicPolicy.ArgNotNull();

        _intrinsicPolicy = intrinsicPolicy;
    }

    public void SetOptimizerPolicy(OptimizerPolicy optimizerPolicy)
    {
        optimizerPolicy = optimizerPolicy.ArgNotNull();

        _optimizerPolicy = optimizerPolicy;
    }

    public void SetSecurityPolicy(SecurityPolicy? securityPolicy)
    {
        _securityPolicy = securityPolicy;
        _securityPolicySet = true;
    }

    public void SetCapabilityPolicy(CapabilityPolicy capabilityPolicy)
    {
        capabilityPolicy = capabilityPolicy.ArgNotNull();

        _capabilityPolicy = capabilityPolicy;
    }

    public void SetOrderRules(IReadOnlyList<OrderRule> orderRules)
    {
        orderRules = orderRules.ArgNotNull();

        _orderRules = orderRules;
    }

    public void SetExtension(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            Thrower.Argument(nameof(key), "Extension key must not be null or empty.");

        value = value.ArgNotNull();

        if (_extensions.ContainsKey(key))
            Thrower.Argument(nameof(key), $"Duplicate extension key '{key}'.");

        _extensions.Add(key, value);
    }

    public bool TryGetExtension(string key, out object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            Thrower.Argument(nameof(key), "Extension key must not be null or empty.");

        return _extensions.TryGetValue(key, out value);
    }

    public DialectDefinition Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
            Thrower.InvalidOpEx("Dialect definition identity must be set before building.");

        if (_modulePolicy == null)
            Thrower.InvalidOpEx("Module policy must be set before building.");

        if (_backendPolicy == null)
            Thrower.InvalidOpEx("Backend policy must be set before building.");

        if (_intrinsicPolicy == null)
            Thrower.InvalidOpEx("Intrinsic policy must be set before building.");

        if (_optimizerPolicy == null)
            Thrower.InvalidOpEx("Optimizer policy must be set before building.");

        if (!_securityPolicySet)
            Thrower.InvalidOpEx("Security policy must be set before building.");

        if (_capabilityPolicy == null)
            Thrower.InvalidOpEx("Capability policy must be set before building.");

        if (_orderRules == null)
            Thrower.InvalidOpEx("Order rules must be set before building.");

        return new DialectDefinition(
            _name,
            _modulePolicy,
            _backendPolicy,
            _intrinsicPolicy,
            _optimizerPolicy,
            _securityPolicy,
            _capabilityPolicy,
            _orderRules,
            _version,
            _baseDialectName,
            _extensions.OrderBy(x => x.Key, StringComparer.Ordinal));
    }
}
