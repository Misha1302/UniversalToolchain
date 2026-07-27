using System.Text.Json;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeManifestJsonSerializer : IRuntimeManifestSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FileDialectRuntimeManifestDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<SerializableManifestDocument>(json, JsonOptions)
                       ?? Thrower.InvalidOpEx<SerializableManifestDocument>("Failed to deserialize runtime manifest JSON.");

        if (string.IsNullOrWhiteSpace(document.AssemblySimpleName))
            Thrower.InvalidOpEx("Runtime manifest must declare assemblySimpleName.");
        if (document.Components == null)
            Thrower.InvalidOpEx("Runtime manifest must declare components.");

        return new FileDialectRuntimeManifestDocument(
            document.AssemblySimpleName,
            document.Components.Select(ReadComponent).ToArray());
    }

    public string Serialize(FileDialectRuntimeManifestDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var payload = new SerializableManifestDocument
        {
            AssemblySimpleName = document.AssemblySimpleName,
            Components = document.Components.Select(static component => new SerializableManifestComponentEntry
            {
                Kind = NormalizeKind(component.Kind),
                CanonicalAlias = component.CanonicalAlias,
                Aliases = component.Aliases,
                ComponentId = component.ComponentId,
                Activation = new SerializableManifestActivationEntry
                {
                    ActivationType = WriteType(component.Activation?.ActivationType
                        ?? throw new InvalidDataException($"Runtime component '{component.ComponentId}' must declare activation metadata.")),
                    RegistrarType = component.Activation?.RegistrarType == null
                        ? null
                        : WriteType(component.Activation.RegistrarType)
                }
            }).ToList()
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static FileDialectRuntimeComponentEntry ReadComponent(SerializableManifestComponentEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.ComponentId))
            Thrower.InvalidOpEx("Every runtime manifest component must declare componentId.");
        if (entry.Activation?.ActivationType == null)
            Thrower.InvalidOpEx($"Runtime component '{entry.ComponentId}' must declare activation.activationType.");

        return new FileDialectRuntimeComponentEntry(
            NormalizeKind(entry.Kind),
            entry.CanonicalAlias ?? string.Empty,
            entry.Aliases ?? [],
            entry.ComponentId,
            new FileRuntimeComponentActivationEntry(
                ReadType(entry.Activation.ActivationType, $"component '{entry.ComponentId}' activationType"),
                entry.Activation.RegistrarType == null
                    ? null
                    : ReadType(entry.Activation.RegistrarType, $"component '{entry.ComponentId}' registrarType")));
    }

    private static RuntimeTypeReference ReadType(SerializableManifestTypeReference reference, string owner)
    {
        if (string.IsNullOrWhiteSpace(reference.AssemblySimpleName) || string.IsNullOrWhiteSpace(reference.TypeFullName))
            Thrower.InvalidOpEx($"Runtime manifest {owner} must declare exact assemblySimpleName and typeFullName.");
        return new RuntimeTypeReference(reference.AssemblySimpleName, reference.TypeFullName);
    }

    private static SerializableManifestTypeReference WriteType(RuntimeTypeReference reference) => new()
    {
        AssemblySimpleName = reference.AssemblySimpleName,
        TypeFullName = reference.TypeFullName
    };

    private static string NormalizeKind(string? kind) =>
        RuntimeComponentKindCodec.Format(RuntimeComponentKindCodec.Parse(kind ?? string.Empty, "runtime manifest"));

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
        public SerializableManifestActivationEntry? Activation { get; init; }
    }

    private sealed class SerializableManifestActivationEntry
    {
        public SerializableManifestTypeReference? ActivationType { get; init; }
        public SerializableManifestTypeReference? RegistrarType { get; init; }
    }

    private sealed class SerializableManifestTypeReference
    {
        public string? AssemblySimpleName { get; init; }
        public string? TypeFullName { get; init; }
    }
}
