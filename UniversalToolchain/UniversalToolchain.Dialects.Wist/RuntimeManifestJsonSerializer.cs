using System.Text.Json;

namespace UniversalToolchain.Dialects.Wist;

public sealed class RuntimeManifestJsonSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FileDialectRuntimeManifestDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<SerializableManifestDocument>(json, JsonOptions)
                       ?? throw new InvalidOperationException("Failed to deserialize runtime manifest JSON.");

        return new FileDialectRuntimeManifestDocument(
            document.DialectFamily ?? string.Empty,
            document.AssemblySimpleName ?? string.Empty,
            (document.Components ?? [])
            .Select(static x => new FileDialectRuntimeComponentEntry(
                x.Kind ?? string.Empty,
                x.CanonicalAlias ?? string.Empty,
                x.Aliases ?? [],
                x.TypeFullName ?? string.Empty))
            .ToList());
    }

    public string Serialize(FileDialectRuntimeManifestDocument document)
    {
        var payload = new SerializableManifestDocument
        {
            DialectFamily = document.DialectFamily,
            AssemblySimpleName = document.AssemblySimpleName,
            Components = document.Components
                .Select(static x => new SerializableManifestComponentEntry
                {
                    Kind = x.Kind,
                    CanonicalAlias = x.CanonicalAlias,
                    Aliases = x.Aliases,
                    TypeFullName = x.TypeFullName
                })
                .ToList()
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private sealed class SerializableManifestDocument
    {
        public string? DialectFamily { get; init; }

        public string? AssemblySimpleName { get; init; }

        public List<SerializableManifestComponentEntry>? Components { get; init; }
    }

    private sealed class SerializableManifestComponentEntry
    {
        public string? Kind { get; init; }

        public string? CanonicalAlias { get; init; }

        public IReadOnlyList<string>? Aliases { get; init; }

        public string? TypeFullName { get; init; }
    }
}
