namespace UniversalToolchain.Dialects.Wist;

public interface IRuntimeManifestFileLocator
{
    IReadOnlyList<string> GetManifestFilePaths();
}
