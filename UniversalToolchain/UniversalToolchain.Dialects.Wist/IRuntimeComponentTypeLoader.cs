namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Strategy for loading runtime component types from manifest metadata.
/// Implementations may use default or custom assembly load contexts.
/// </summary>
public interface IRuntimeComponentTypeLoader
{
    Type LoadType(RuntimeComponentManifestEntry entry);
}
