using System.Text.Json;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeManifestJsonSerializer : IRuntimeManifestSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FileDialectRuntimeManifestDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<SerializableManifestDocument>(json, _jsonOptions)
                       ?? Thrower.InvalidOpEx<SerializableManifestDocument>("Failed to deserialize runtime manifest JSON.");

        return new FileDialectRuntimeManifestDocument(
            document.AssemblySimpleName ?? string.Empty,
            (document.Components ?? [])
            .Select(x => new FileDialectRuntimeComponentEntry(
                RuntimeComponentKindCodec.Format(RuntimeComponentKindCodec.Parse(x.Kind ?? string.Empty, "runtime manifest")),
                x.CanonicalAlias ?? string.Empty,
                x.Aliases ?? [],
                ResolveComponentId(x),
                ResolveActivation(x)))
            .ToList());
    }

    public string Serialize(FileDialectRuntimeManifestDocument document)
    {
        var payload = new SerializableManifestDocument
        {
            AssemblySimpleName = document.AssemblySimpleName,
            Components = document.Components
                .Select(static x => new SerializableManifestComponentEntry
                {
                    Kind = RuntimeComponentKindCodec.Format(RuntimeComponentKindCodec.Parse(x.Kind ?? string.Empty, "runtime manifest")),
                    CanonicalAlias = x.CanonicalAlias,
                    Aliases = x.Aliases,
                    ComponentId = x.ComponentId,
                    Activation = x.Activation == null
                        ? null
                        : new SerializableManifestActivationEntry
                        {
                            ActivationTypeFullName = x.Activation.ActivationTypeFullName,
                            RegistrarTypeFullName = x.Activation.RegistrarTypeFullName
                        }
                })
                .ToList()
        };

        return JsonSerializer.Serialize(payload, _jsonOptions);
    }

    private static string ResolveComponentId(SerializableManifestComponentEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.ComponentId))
            return entry.ComponentId;

        var kind = RuntimeComponentKindCodec.Parse(entry.Kind ?? string.Empty, "runtime manifest");
        var alias = entry.CanonicalAlias ?? string.Empty;
        return RuntimeComponentIdFactory.Create(kind, alias).Value;
    }

    private static FileRuntimeComponentActivationEntry? ResolveActivation(SerializableManifestComponentEntry entry)
    {
        if (entry.Activation != null)
            return new FileRuntimeComponentActivationEntry(
                entry.Activation.ActivationTypeFullName ?? string.Empty,
                entry.Activation.RegistrarTypeFullName);

        if (!string.IsNullOrWhiteSpace(entry.TypeFullName))
            return new FileRuntimeComponentActivationEntry(entry.TypeFullName);

        return null;
    }

    private sealed class SerializableManifestDocument
    {
        public string? AssemblySimpleName { get; init; }

        public List<SerializableManifestComponentEntry>? Components { get; init; }
    }

    private sealed class SerializableManifestComponentEntry
    {
        public string? Kind { get; init; }

        public string? CanonicalAlias { get; init; }

        public IReadOnlyList<string>? Aliases { get; init; }

        public string? ComponentId { get; init; }

        public string? TypeFullName { get; init; }

        public SerializableManifestActivationEntry? Activation { get; init; }
    }

    private sealed class SerializableManifestActivationEntry
    {
        public string? ActivationTypeFullName { get; init; }

        public string? RegistrarTypeFullName { get; init; }
    }
}
