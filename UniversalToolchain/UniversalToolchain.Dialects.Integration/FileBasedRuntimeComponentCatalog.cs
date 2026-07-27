using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class FileBasedRuntimeComponentCatalog : IRuntimeComponentCatalog
{
    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _backendsByAlias;
    private readonly IReadOnlyList<RuntimeComponentManifestEntry> _backendsInOrder;
    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _modulesByAlias;
    private readonly IReadOnlyList<RuntimeComponentManifestEntry> _modulesInOrder;
    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _optimizersByAlias;
    private readonly IReadOnlyList<RuntimeComponentManifestEntry> _optimizersInOrder;

    public FileBasedRuntimeComponentCatalog(
        IRuntimeManifestFileLocator manifestFileLocator,
        IRuntimeManifestSerializer manifestSerializer)
    {
        manifestFileLocator = manifestFileLocator.ArgNotNull();

        manifestSerializer = manifestSerializer.ArgNotNull();

        var entries = LoadEntries(manifestFileLocator.GetManifestFilePaths(), manifestSerializer);

        _modulesInOrder = Sort(entries.Where(static x => x.Kind == RuntimeComponentKind.FrontendModule));
        _optimizersInOrder = Sort(entries.Where(static x => x.Kind == RuntimeComponentKind.Optimizer));
        _backendsInOrder = Sort(entries.Where(static x => x.Kind == RuntimeComponentKind.Backend));

        _modulesByAlias = CreateAliasMap(_modulesInOrder, "module");
        _optimizersByAlias = CreateAliasMap(_optimizersInOrder, "optimizer");
        _backendsByAlias = CreateAliasMap(_backendsInOrder, "backend");
    }

    public bool TryResolveModule(string alias, out RuntimeComponentManifestEntry? entry) => TryResolve(_modulesByAlias, alias, out entry);

    public bool TryResolveOptimizer(string alias, out RuntimeComponentManifestEntry? entry) => TryResolve(_optimizersByAlias, alias, out entry);

    public bool TryResolveBackend(string alias, out RuntimeComponentManifestEntry? entry) => TryResolve(_backendsByAlias, alias, out entry);

    public IReadOnlyList<RuntimeComponentManifestEntry> GetModulesInDeterministicOrder() => _modulesInOrder;

    public IReadOnlyList<RuntimeComponentManifestEntry> GetOptimizersInDeterministicOrder() => _optimizersInOrder;

    public IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder() => _backendsInOrder;

    internal static IReadOnlyList<RuntimeComponentManifestEntry> LoadEntries(
        IEnumerable<string> manifestPaths,
        IRuntimeManifestSerializer manifestSerializer)
    {
        var entries = new List<RuntimeComponentManifestEntry>();

        foreach (var manifestPath in manifestPaths.OrderBy(static x => x, StringComparer.Ordinal))
        {
            if (!File.Exists(manifestPath))
                continue;

            var document = manifestSerializer.Deserialize(File.ReadAllText(manifestPath));
            var assemblySimpleName = NormalizeRequiredText(
                document.AssemblySimpleName,
                nameof(manifestPaths),
                $"Runtime manifest '{manifestPath}' has an empty assemblySimpleName.");

            entries.AddRange(document.Components.Select(component => ToRuntimeEntry(component, assemblySimpleName, manifestPath)));
        }

        ValidateUniqueIds(entries);
        return entries;
    }

    private static RuntimeComponentManifestEntry ToRuntimeEntry(
        FileDialectRuntimeComponentEntry component,
        string assemblySimpleName,
        string manifestPath)
    {
        var kind = RuntimeComponentKindCodec.Parse(component.Kind, manifestPath);
        var canonicalAlias = component.CanonicalAlias;
        if (string.IsNullOrWhiteSpace(component.ComponentId))
            Thrower.InvalidOpEx($"Runtime component '{canonicalAlias}' in manifest '{manifestPath}' must declare componentId.");
        if (component.Activation == null)
            Thrower.InvalidOpEx($"Runtime component '{component.ComponentId}' in manifest '{manifestPath}' must declare exact activation metadata.");
        var componentId = new RuntimeComponentId(component.ComponentId);

        return Normalize(new RuntimeComponentManifestEntry(
            kind,
            canonicalAlias,
            component.Aliases,
            componentId,
            assemblySimpleName,
            ToRuntimeActivation(component.Activation)));
    }

    private static bool TryResolve(IReadOnlyDictionary<string, RuntimeComponentManifestEntry> map, string alias, out RuntimeComponentManifestEntry? entry)
    {
        if (string.IsNullOrWhiteSpace(alias))
            Thrower.Argument(nameof(alias), "Alias must not be empty.");

        return map.TryGetValue(alias.Trim(), out entry);
    }

    private static IReadOnlyList<RuntimeComponentManifestEntry> Sort(IEnumerable<RuntimeComponentManifestEntry> entries)
    {
        return entries
            .OrderBy(static x => x.CanonicalAlias, StringComparer.Ordinal)
            .ThenBy(static x => x.ComponentId.Value, StringComparer.Ordinal)
            .ToList();
    }

    private static RuntimeComponentManifestEntry Normalize(RuntimeComponentManifestEntry entry)
    {
        entry = entry.ArgNotNull();

        var canonical = NormalizeRequiredText(entry.CanonicalAlias, nameof(entry), "Canonical alias must not be empty.");

        var assemblySimpleName = NormalizeRequiredText(entry.AssemblySimpleName, nameof(entry), "AssemblySimpleName must not be empty.");

        var aliases = NormalizeAliases(entry.Aliases, canonical);

        return entry with
        {
            CanonicalAlias = canonical,
            Aliases = aliases,
            AssemblySimpleName = assemblySimpleName,
            ComponentId = new RuntimeComponentId(entry.ComponentId.Value.Trim()),
            Activation = NormalizeActivation(entry.Activation, assemblySimpleName)
        };
    }

    private static RuntimeComponentActivationInfo ToRuntimeActivation(FileRuntimeComponentActivationEntry? activation)
    {
        activation = activation.NotNull(nameof(activation));
        return new RuntimeComponentActivationInfo(activation.ActivationType, activation.RegistrarType);
    }

    private static RuntimeComponentActivationInfo NormalizeActivation(RuntimeComponentActivationInfo? activation, string ownerAssemblySimpleName)
    {
        activation = activation.NotNull(nameof(activation));

        var activationType = NormalizeTypeReference(
            activation.ActivationType,
            ownerAssemblySimpleName,
            nameof(activation),
            "ActivationTypeFullName must not be empty when activation metadata is provided.");
        var registrarType = activation.RegistrarType == null
            ? null
            : NormalizeTypeReference(
                activation.RegistrarType,
                ownerAssemblySimpleName,
                nameof(activation),
                "RegistrarTypeFullName must not be empty when activation metadata is provided.");

        return new RuntimeComponentActivationInfo(
            activationType,
            registrarType);
    }

    private static RuntimeTypeReference NormalizeTypeReference(
        RuntimeTypeReference typeReference,
        string ownerAssemblySimpleName,
        string paramName,
        string emptyTypeMessage)
    {
        typeReference = typeReference.NotNull(paramName);

        var typeFullName = NormalizeRequiredText(typeReference.TypeFullName, paramName, emptyTypeMessage);

        var assemblySimpleName = NormalizeRequiredText(
            typeReference.AssemblySimpleName,
            paramName,
            "Runtime activation type must declare exact assemblySimpleName.");

        return new RuntimeTypeReference(assemblySimpleName, typeFullName);
    }

    private static string NormalizeRequiredText(string? value, string paramName, string message)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            Thrower.Argument(paramName, message);

        return normalized.NotNull(paramName);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static List<string> NormalizeAliases(IEnumerable<string?>? aliases, string canonicalAlias)
    {
        if (aliases == null)
            return [];

        var result = new List<string>();
        foreach (var alias in aliases)
        {
            var normalized = NormalizeOptionalText(alias);
            if (normalized == null || string.Equals(normalized, canonicalAlias, StringComparison.Ordinal))
                continue;

            result.Add(normalized);
        }

        return result
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyDictionary<string, RuntimeComponentManifestEntry> CreateAliasMap(IEnumerable<RuntimeComponentManifestEntry> entries, string kindName)
    {
        var map = new Dictionary<string, RuntimeComponentManifestEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            foreach (var alias in entry.AllAliases)
            {
                if (!map.TryAdd(alias, entry))
                    Thrower.InvalidOpEx($"Duplicate runtime {kindName} alias '{alias}'.");
            }
        }

        return map;
    }

    private static void ValidateUniqueIds(IEnumerable<RuntimeComponentManifestEntry> entries)
    {
        var ownersById = new Dictionary<RuntimeComponentId, RuntimeComponentManifestEntry>();

        foreach (var entry in entries)
        {
            if (ownersById.TryGetValue(entry.ComponentId, out var existing))
                Thrower.InvalidOpEx(
                    $"Duplicate runtime component id '{entry.ComponentId}' for aliases '{existing.CanonicalAlias}' and '{entry.CanonicalAlias}'.");

            ownersById.Add(entry.ComponentId, entry);
        }
    }
}