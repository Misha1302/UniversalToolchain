using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.FeatureSdk;

/// <summary>
/// Opaque identity issued by <see cref="LanguagePackageRegistry"/> for one concrete package registration.
/// Callers can inspect provenance, but cannot manufacture a trusted identity outside the registry.
/// </summary>
public sealed class LanguagePackageRegistrationIdentity
{
    private readonly object? _implementation;

    internal LanguagePackageRegistrationIdentity(
        LanguagePackageDescriptor descriptor,
        object? implementation)
    {
        PackageId = descriptor.Id;
        PackageVersion = descriptor.Version;
        ManifestSha256 = LanguageFeatureManifestSerializer.ComputeSha256(descriptor);
        _implementation = implementation;
        ImplementationType = implementation?.GetType();
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

    /// <summary>
    /// Returns true only for the exact implementation object registered in the package registry.
    /// Matching package id/version, manifest or implementation type is intentionally insufficient.
    /// </summary>
    public bool IsImplementation(object expectedImplementation)
    {
        ArgumentNullException.ThrowIfNull(expectedImplementation);
        return ReferenceEquals(_implementation, expectedImplementation);
    }
}
