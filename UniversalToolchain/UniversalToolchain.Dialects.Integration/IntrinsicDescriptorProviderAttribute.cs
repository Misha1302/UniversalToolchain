using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class IntrinsicDescriptorProviderAttribute : Attribute
{
    public IntrinsicDescriptorProviderAttribute(Type providerType)
    {
        providerType = providerType.ArgNotNull();

        ProviderType = providerType;
    }

    public Type ProviderType { get; }
}