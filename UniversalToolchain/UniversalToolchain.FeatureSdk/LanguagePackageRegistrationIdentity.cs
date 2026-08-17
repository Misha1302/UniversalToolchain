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
    /// Returns whether this opaque registration identity is bound to the exact supplied package instance.
    /// This is a provenance check only; it does not perform discovery or substitute another implementation.
    /// </summary>
    public bool IsImplementationInstance(object expectedImplementation)
    {
        ArgumentNullException.ThrowIfNull(expectedImplementation);
        return ReferenceEquals(_implementation, expectedImplementation);
    }

    /// <summary>
    /// Returns the exact implementation captured when this registration identity was issued.
    /// Descriptor-only registrations and registrations of a different concrete type fail closed.
    /// </summary>
    public TImplementation GetRequiredImplementation<TImplementation>()
        where TImplementation : class
    {
        if (_implementation is TImplementation implementation && _implementation.GetType() == typeof(TImplementation))
            return implementation;

        throw new InvalidOperationException(
            $"Package '{PackageId.Value}' version '{PackageVersion.Value}' was not registered with exact implementation type '{typeof(TImplementation).FullName}'.");
    }
}
