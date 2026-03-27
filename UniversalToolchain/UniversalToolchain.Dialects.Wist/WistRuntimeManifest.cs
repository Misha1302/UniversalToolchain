using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

public sealed class WistRuntimeManifest : IWistRuntimeManifest
{
    private const string WistDialectFamily = "wist";

    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _backendsByAlias;
    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _modulesByAlias;
    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _optimizersByAlias;

    public WistRuntimeManifest()
        : this(new DefaultRuntimeManifestFileLocator(), new RuntimeManifestJsonSerializer())
    {
    }

    public WistRuntimeManifest(
        IRuntimeManifestFileLocator manifestFileLocator,
        RuntimeManifestJsonSerializer jsonSerializer)
    {
        if (manifestFileLocator == null)
            Thrower.ArgumentNull(nameof(manifestFileLocator));

        if (jsonSerializer == null)
            Thrower.ArgumentNull(nameof(jsonSerializer));

        var entries = LoadEntries(manifestFileLocator.GetManifestFilePaths(), jsonSerializer);

        Modules = Sort(entries.Where(static x => x.Kind == RuntimeComponentKind.FrontendModule));
        Optimizers = Sort(entries.Where(static x => x.Kind == RuntimeComponentKind.Optimizer));
        Backends = Sort(entries.Where(static x => x.Kind == RuntimeComponentKind.Backend));

        _modulesByAlias = CreateAliasMap(Modules, nameof(Modules));
        _optimizersByAlias = CreateAliasMap(Optimizers, nameof(Optimizers));
        _backendsByAlias = CreateAliasMap(Backends, nameof(Backends));
    }

    public IReadOnlyCollection<RuntimeComponentManifestEntry> Modules { get; }

    public IReadOnlyCollection<RuntimeComponentManifestEntry> Optimizers { get; }

    public IReadOnlyCollection<RuntimeComponentManifestEntry> Backends { get; }

    public bool TryResolveModule(string alias, out RuntimeComponentManifestEntry? entry) => TryResolve(_modulesByAlias, alias, out entry);

    public bool TryResolveOptimizer(string alias, out RuntimeComponentManifestEntry? entry) => TryResolve(_optimizersByAlias, alias, out entry);

    public bool TryResolveBackend(string backendId, out RuntimeComponentManifestEntry? entry) => TryResolve(_backendsByAlias, backendId, out entry);

    public IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder() =>
        Backends.OrderBy(static x => x.CanonicalAlias, StringComparer.Ordinal)
            .ThenBy(static x => x.TypeReference.TypeFullName, StringComparer.Ordinal)
            .ToList();

    internal static IReadOnlyList<RuntimeComponentManifestEntry> LoadEntries(
        IEnumerable<string> manifestPaths,
        RuntimeManifestJsonSerializer jsonSerializer)
    {
        var entries = new List<RuntimeComponentManifestEntry>();

        foreach (var manifestPath in manifestPaths.OrderBy(static x => x, StringComparer.Ordinal))
        {
            if (!File.Exists(manifestPath))
                continue;

            var document = jsonSerializer.Deserialize(File.ReadAllText(manifestPath));
            if (!string.Equals(document.DialectFamily?.Trim(), WistDialectFamily, StringComparison.Ordinal))
                continue;

            var assemblySimpleName = document.AssemblySimpleName?.Trim();
            if (string.IsNullOrWhiteSpace(assemblySimpleName))
                Thrower.Argument(nameof(manifestPaths), $"Runtime manifest '{manifestPath}' has an empty assemblySimpleName.");

            foreach (var component in document.Components ?? [])
            {
                entries.Add(ToRuntimeEntry(component, assemblySimpleName!, manifestPath));
            }
        }

        return entries;
    }

    private static RuntimeComponentManifestEntry ToRuntimeEntry(
        FileDialectRuntimeComponentEntry component,
        string assemblySimpleName,
        string manifestPath)
    {
        var kind = ParseKind(component.Kind, manifestPath);

        return Normalize(new RuntimeComponentManifestEntry(
            kind,
            component.CanonicalAlias,
            component.Aliases,
            new RuntimeTypeReference(assemblySimpleName, component.TypeFullName)));
    }

    private static RuntimeComponentKind ParseKind(string kind, string manifestPath)
    {
        return kind switch
        {
            "FrontendModule" => RuntimeComponentKind.FrontendModule,
            "Optimizer" => RuntimeComponentKind.Optimizer,
            "Backend" => RuntimeComponentKind.Backend,
            _ => Thrower.InvalidOpEx<RuntimeComponentKind>($"Unsupported runtime component kind '{kind}' in '{manifestPath}'.")
        };
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
            .ThenBy(static x => x.TypeReference.TypeFullName, StringComparer.Ordinal)
            .ToList();
    }

    private static RuntimeComponentManifestEntry Normalize(RuntimeComponentManifestEntry entry)
    {
        if (entry == null)
            Thrower.ArgumentNull(nameof(entry));

        var canonical = entry.CanonicalAlias?.Trim();
        if (string.IsNullOrWhiteSpace(canonical))
            Thrower.Argument(nameof(entry), "Canonical alias must not be empty.");

        var assemblySimpleName = entry.TypeReference.AssemblySimpleName?.Trim();
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
            Thrower.Argument(nameof(entry), "TypeReference.AssemblySimpleName must not be empty.");

        var typeFullName = entry.TypeReference.TypeFullName?.Trim();
        if (string.IsNullOrWhiteSpace(typeFullName))
            Thrower.Argument(nameof(entry), "TypeReference.TypeFullName must not be empty.");

        var aliases = (entry.Aliases ?? [])
            .Select(static x => x?.Trim())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();

        aliases.RemoveAll(x => string.Equals(x, canonical, StringComparison.Ordinal));

        return entry with
        {
            CanonicalAlias = canonical,
            Aliases = aliases,
            TypeReference = new RuntimeTypeReference(assemblySimpleName, typeFullName)
        };
    }

    private static IReadOnlyDictionary<string, RuntimeComponentManifestEntry> CreateAliasMap(IEnumerable<RuntimeComponentManifestEntry> entries, string collectionName)
    {
        var map = new Dictionary<string, RuntimeComponentManifestEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            foreach (var alias in entry.AllAliases)
            {
                if (!map.TryAdd(alias, entry))
                    Thrower.InvalidOpEx($"Duplicate runtime component alias '{alias}' in '{collectionName}'.");
            }
        }

        return map;
    }
}
