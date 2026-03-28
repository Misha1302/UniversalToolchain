namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeManifestSerializer
{
    FileDialectRuntimeManifestDocument Deserialize(string json);

    string Serialize(FileDialectRuntimeManifestDocument document);
}