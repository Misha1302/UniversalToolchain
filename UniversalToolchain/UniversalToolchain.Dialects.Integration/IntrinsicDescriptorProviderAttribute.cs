using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class IntrinsicDescriptorProviderAttribute : Attribute
{
    public IntrinsicDescriptorProviderAttribute(Type providerType)
    {
        if (providerType == null)
            Thrower.ArgumentNull(nameof(providerType));

        ProviderType = providerType;
    }

    public Type ProviderType { get; }
}
