namespace UniversalToolchain.Capabilities.Core;

public sealed record CapabilityProviderDescriptor(
    Type RuntimeComponentImplementationType,
    Type ProviderType);