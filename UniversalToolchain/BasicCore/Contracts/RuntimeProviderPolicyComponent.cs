namespace BasicCore.Contracts;

public sealed class RuntimeProviderPolicyComponent : IRuntimeProviderPolicyComponent
{
    private readonly IReadOnlyCollection<Type> _allowedRuntimeProviderTypes;

    public RuntimeProviderPolicyComponent(IEnumerable<Type> allowedRuntimeProviderTypes)
    {
        allowedRuntimeProviderTypes = allowedRuntimeProviderTypes.ArgNotNull();

        _allowedRuntimeProviderTypes = allowedRuntimeProviderTypes
            .Select(type => type.ArgNotNull())
            .Distinct()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    public string ComponentId => "runtime-provider-policy";

    public IReadOnlyCollection<Type> AllowedRuntimeProviderTypes => _allowedRuntimeProviderTypes;
}
