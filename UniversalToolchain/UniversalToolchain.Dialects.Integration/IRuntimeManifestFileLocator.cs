namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeManifestFileLocator
{
    IReadOnlyList<string> GetManifestFilePaths();
}
