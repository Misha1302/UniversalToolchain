namespace UniversalToolchain.Capabilities.Abstractions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class DialectCapabilityProviderAttribute : Attribute
{
    public DialectCapabilityProviderAttribute(Type providerType)
    {
        ProviderType = providerType;
    }

    public Type ProviderType { get; }
}
