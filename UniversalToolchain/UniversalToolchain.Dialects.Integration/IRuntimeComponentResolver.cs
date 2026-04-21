namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeComponentResolver
{
    /// <summary>
    /// Resolves a manifest-selected runtime component and returns an activation-capable descriptor.
    /// Manifest metadata remains authoritative; reflection confirms a matching export and supplies the activation type.
    /// </summary>
    RuntimeComponentDescriptor Resolve(RuntimeComponentManifestEntry entry);
}
