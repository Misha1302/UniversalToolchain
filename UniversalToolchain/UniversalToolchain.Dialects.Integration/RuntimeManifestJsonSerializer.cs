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
                            ActivationType = CreateSerializableTypeReference(x.Activation.ActivationType),
                            RegistrarType = CreateSerializableTypeReference(x.Activation.RegistrarType),
                            ActivationTypeFullName = x.Activation.ActivationType.TypeFullName,
                            ActivationAssemblySimpleName = ResolveAssemblySimpleNameForSerialization(x.Activation.ActivationType),
                            RegistrarTypeFullName = x.Activation.RegistrarType?.TypeFullName,
                            RegistrarAssemblySimpleName = ResolveAssemblySimpleNameForSerialization(x.Activation.RegistrarType)
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
        {
            var activationType = ResolveTypeReference(
                entry.Activation.ActivationType,
                entry.Activation.ActivationTypeFullName,
                entry.Activation.ActivationAssemblySimpleName);
            var registrarType = ResolveTypeReference(
                entry.Activation.RegistrarType,
                entry.Activation.RegistrarTypeFullName,
                entry.Activation.RegistrarAssemblySimpleName,
                true);

            return new FileRuntimeComponentActivationEntry(
                activationType.NotNull(nameof(entry)),
                registrarType);
        }

        if (!string.IsNullOrWhiteSpace(entry.TypeFullName))
            return new FileRuntimeComponentActivationEntry(entry.TypeFullName);

        return null;
    }

    private static SerializableManifestTypeReference? CreateSerializableTypeReference(RuntimeTypeReference? typeReference)
    {
        if (typeReference == null)
            return null;

        return new SerializableManifestTypeReference
        {
            AssemblySimpleName = ResolveAssemblySimpleNameForSerialization(typeReference),
            TypeFullName = typeReference.TypeFullName
        };
    }

    private static RuntimeTypeReference? ResolveTypeReference(
        SerializableManifestTypeReference? structuredReference,
        string? legacyTypeFullName,
        string? legacyAssemblySimpleName,
        bool allowMissingType = false)
    {
        var typeFullName = structuredReference?.TypeFullName ?? legacyTypeFullName ?? string.Empty;
        if (allowMissingType && string.IsNullOrWhiteSpace(typeFullName))
            return null;

        var assemblySimpleName = structuredReference?.AssemblySimpleName
                                 ?? legacyAssemblySimpleName
                                 ?? RuntimeAssemblyIdentity.UnspecifiedAssemblySimpleName;

        return new RuntimeTypeReference(assemblySimpleName, typeFullName);
    }

    private static string? ResolveAssemblySimpleNameForSerialization(RuntimeTypeReference? typeReference)
    {
        if (typeReference == null)
            return null;

        return string.Equals(typeReference.AssemblySimpleName, RuntimeAssemblyIdentity.UnspecifiedAssemblySimpleName, StringComparison.Ordinal)
            ? null
            : typeReference.AssemblySimpleName;
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
        public SerializableManifestTypeReference? ActivationType { get; init; }

        public SerializableManifestTypeReference? RegistrarType { get; init; }

        public string? ActivationTypeFullName { get; init; }

        public string? ActivationAssemblySimpleName { get; init; }

        public string? RegistrarTypeFullName { get; init; }

        public string? RegistrarAssemblySimpleName { get; init; }
    }

    private sealed class SerializableManifestTypeReference
    {
        public string? AssemblySimpleName { get; init; }

        public string? TypeFullName { get; init; }
    }
}