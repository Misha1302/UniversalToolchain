using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.FeatureSdk;

/// <summary>
/// Opaque identity issued by <see cref="LanguagePackageRegistry"/> for one concrete package registration.
/// Callers can inspect provenance, but cannot manufacture a trusted identity outside the registry.
/// </summary>
public sealed class LanguagePackageRegistrationIdentity
{
    internal LanguagePackageRegistrationIdentity(
        LanguagePackageDescriptor descriptor,
        Type? implementationType)
    {
        PackageId = descriptor.Id;
        PackageVersion = descriptor.Version;
        ManifestSha256 = LanguageFeatureManifestSerializer.ComputeSha256(descriptor);
        ImplementationType = implementationType;
    }

    public LanguagePackageId PackageId { get; }
    public LanguageVersion PackageVersion { get; }
    public string ManifestSha256 { get; }

    /// <summary>
    /// Concrete package implementation registered through <c>AddPackage(ILanguageFeaturePackage)</c>.
    /// It is null for descriptor-only registrations, which are unsuitable for providers that require
    /// implementation provenance.
    /// </summary>
    public Type? ImplementationType { get; }

    public bool IsImplementation(Type expectedType)
    {
        ArgumentNullException.ThrowIfNull(expectedType);
        return ImplementationType == expectedType;
    }
}
