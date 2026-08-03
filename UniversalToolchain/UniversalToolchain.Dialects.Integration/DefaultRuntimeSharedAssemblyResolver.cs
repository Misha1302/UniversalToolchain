using System.Collections.Immutable;
using System.Reflection;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Resolves only assemblies explicitly registered by the host and validates configured copies fail-closed.
/// </summary>
public sealed class DefaultRuntimeSharedAssemblyResolver : IRuntimeSharedAssemblyResolver
{
    private readonly ImmutableDictionary<string, RuntimeSharedAssemblyDescriptor> _descriptorsBySimpleName;

    public DefaultRuntimeSharedAssemblyResolver(IEnumerable<RuntimeSharedAssemblyDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var builder = ImmutableDictionary.CreateBuilder<string, RuntimeSharedAssemblyDescriptor>(StringComparer.Ordinal);
        foreach (var supplied in descriptors)
        {
            ArgumentNullException.ThrowIfNull(supplied);
            var canonical = RuntimeSharedAssemblyDescriptor.Create(supplied.HostAssembly);
            ValidateSuppliedDescriptor(supplied, canonical);

            if (!builder.TryGetValue(canonical.Identity.Name, out var existing))
            {
                builder.Add(canonical.Identity.Name, canonical);
                continue;
            }

            if (ReferenceEquals(existing.HostAssembly, canonical.HostAssembly) && existing == canonical)
                continue;

            throw new InvalidOperationException(
                $"Conflicting runtime shared assembly registrations for simple name '{canonical.Identity.Name}': " +
                $"'{existing.Identity}' at '{existing.HostAssemblyPath}' (SHA-256 {existing.Sha256}) and " +
                $"'{canonical.Identity}' at '{canonical.HostAssemblyPath}' (SHA-256 {canonical.Sha256}).");
        }

        _descriptorsBySimpleName = builder.ToImmutable();
    }

    public RuntimeSharedAssemblyResolution Resolve(AssemblyName requestedIdentity, string configuredAssemblyPath)
    {
        ArgumentNullException.ThrowIfNull(requestedIdentity);
        if (string.IsNullOrWhiteSpace(requestedIdentity.Name))
            throw new ArgumentException("Requested assembly identity must contain a simple name.", nameof(requestedIdentity));
        if (string.IsNullOrWhiteSpace(configuredAssemblyPath))
            throw new ArgumentException("Configured assembly path must not be empty.", nameof(configuredAssemblyPath));
        if (!Path.IsPathRooted(configuredAssemblyPath))
            throw new ArgumentException($"Configured assembly path '{configuredAssemblyPath}' must be absolute.", nameof(configuredAssemblyPath));

        if (!_descriptorsBySimpleName.TryGetValue(requestedIdentity.Name, out var descriptor))
            return RuntimeSharedAssemblyResolution.NotShared;

        var requested = RuntimeAssemblyIdentity.FromAssemblyName(requestedIdentity);
        if (requested != descriptor.Identity)
            throw IdentityMismatch("requested", descriptor, requested, configuredAssemblyPath);

        var path = Path.GetFullPath(configuredAssemblyPath);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Configured runtime shared assembly '{path}' does not exist.", path);

        AssemblyName configuredAssemblyName;
        try
        {
            configuredAssemblyName = AssemblyName.GetAssemblyName(path);
        }
        catch (BadImageFormatException exception)
        {
            throw new InvalidOperationException($"Configured runtime shared assembly '{path}' is not a valid managed assembly.", exception);
        }

        var configuredIdentity = RuntimeAssemblyIdentity.FromAssemblyName(configuredAssemblyName);
        if (configuredIdentity != descriptor.Identity)
            throw IdentityMismatch("configured", descriptor, configuredIdentity, path);

        var configuredSha256 = RuntimeSharedAssemblyDescriptor.ComputeSha256(path);
        if (!string.Equals(configuredSha256, descriptor.Sha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Configured runtime shared assembly '{descriptor.Identity}' at '{path}' failed strict content integrity validation. " +
                $"Expected SHA-256 '{descriptor.Sha256}' from host assembly '{descriptor.HostAssemblyPath}', actual SHA-256 '{configuredSha256}'.");
        }

        return RuntimeSharedAssemblyResolution.Shared(descriptor.HostAssembly);
    }

    private static void ValidateSuppliedDescriptor(
        RuntimeSharedAssemblyDescriptor supplied,
        RuntimeSharedAssemblyDescriptor canonical)
    {
        if (supplied.Identity != canonical.Identity ||
            !string.Equals(Path.GetFullPath(supplied.HostAssemblyPath), canonical.HostAssemblyPath, StringComparison.Ordinal) ||
            !string.Equals(supplied.Sha256, canonical.Sha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Runtime shared assembly descriptor for '{canonical.Identity}' is not a valid immutable snapshot of " +
                $"host assembly '{canonical.HostAssemblyPath}'.");
        }
    }

    private static InvalidOperationException IdentityMismatch(
        string actualKind,
        RuntimeSharedAssemblyDescriptor expected,
        RuntimeAssemblyIdentity actual,
        string configuredPath) =>
        new(
            $"Runtime shared assembly identity mismatch for '{expected.Identity.Name}' at '{configuredPath}'. " +
            $"Expected host identity '{expected.Identity}', {actualKind} identity '{actual}'. Isolated fallback is forbidden.");
}
