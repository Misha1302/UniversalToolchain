using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

public sealed class WistRuntimeManifest : IWistRuntimeManifest
{
    private static readonly IReadOnlyList<RuntimeComponentManifestEntry> ModuleEntries =
    [
        Create(RuntimeComponentKind.FrontendModule, "Arithmetic", [], "ArithmeticModule", "ArithmeticModule.Module.ArithmeticModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "Comments", [], "CommentsModule", "CommentsModule.CommentsModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "Conditions", [], "ConditionsModule", "ConditionsModule.Module.ConditionsModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "ComparisonConditions", [], "ConditionsModule", "ConditionsModule.Enums.ComparisonOperations"),
        Create(RuntimeComponentKind.FrontendModule, "BooleanConditions", [], "ConditionsModule", "ConditionsModule.Enums.BooleanOperations"),
        Create(RuntimeComponentKind.FrontendModule, "CSharpInterop", [], "CSharpInteropModule", "CSharpInteropModule.Module.CSharpInteropModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "Equality", [], "EqualityModule", "EqualityModule.EqualityModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "Identifier", [], "IdentifierModule", "IdentifierModule.IdentifierModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "InternalPreprocessorLexemes", [], "InternalPreprocessorLexemesModule", "InternalPreprocessorLexemesModule.InternalPreprocessorLexemesModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "Labels", [], "LabelsModule", "LabelsModule.Module.LabelsModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "Loops", [], "LoopsModule", "LoopsModule.Module.LoopsModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "NativeTypes", [], "NativeMathModule", "NativeMathModule.NativeTypesModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "Numbers", [], "NumbersModule", "NumbersModule.Module.NumbersModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "ParametersSetter", [], "ParametersSetterModule", "ParametersSetterModule.ParametersSetterModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "Scopes", [], "ScopesModule", "ScopesModule.Module.ScopesModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "SemicolonAsNewLine", [], "SemicolonAsNewLineModule", "SemicolonAsNewLineModule.SemicolonAsNewLineModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "Variables", [], "VariablesModule", "VariablesModule.VariablesModuleImpl"),
        Create(RuntimeComponentKind.FrontendModule, "Whitespaces", [], "WhitespacesModule", "WhitespacesModule.WhitespaceModuleImpl")
    ];

    private static readonly IReadOnlyList<RuntimeComponentManifestEntry> OptimizerEntries =
    [
        Create(RuntimeComponentKind.Optimizer, "ArithmeticOptimization", [], "NativeMathModule", "NativeMathModule.ArithmeticOptimizerModule"),
        Create(RuntimeComponentKind.Optimizer, "BooleanOptimization", [], "ConditionsModule", "ConditionsModule.Optimizers.BooleanOptimizerModule"),
        Create(RuntimeComponentKind.Optimizer, "ComparisonIntrinsicOptimization", [], "ConditionsModule", "ConditionsModule.Optimizers.ComparisonIntrinsicOptimizerModule"),
        Create(RuntimeComponentKind.Optimizer, "EGraphOptimization", [], "NativeMathModule", "NativeMathModule.EGraphOptimizerModule"),
        Create(RuntimeComponentKind.Optimizer, "LocalVariablesOptimization", [], "LocalVariablesOptimizerModule", "LocalVariablesOptimizerModule.LocalVariablesOptimizer"),
        Create(RuntimeComponentKind.Optimizer, "NativeCilOptimization", [], "NativeMathModule", "NativeMathModule.NativeCilOptimizerModule"),
        Create(RuntimeComponentKind.Optimizer, "NativeTypesOptimization", [], "NativeMathModule", "NativeMathModule.NativeTypesOptimizerModule")
    ];

    private static readonly IReadOnlyList<RuntimeComponentManifestEntry> BackendEntries =
    [
        Create(RuntimeComponentKind.Backend, "cil", ["compiler"], "UniversalToolchain.Dialects.Wist", "UniversalToolchain.Dialects.Wist.WistCilBackendDeclaration"),
        Create(RuntimeComponentKind.Backend, "interpreter", [], "UniversalToolchain.Dialects.Wist", "UniversalToolchain.Dialects.Wist.WistInterpreterBackendDeclaration")
    ];

    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _backendsByAlias;
    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _modulesByAlias;
    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _optimizersByAlias;

    public WistRuntimeManifest()
        : this(ModuleEntries, OptimizerEntries, BackendEntries)
    {
    }

    internal WistRuntimeManifest(
        IEnumerable<RuntimeComponentManifestEntry> modules,
        IEnumerable<RuntimeComponentManifestEntry> optimizers,
        IEnumerable<RuntimeComponentManifestEntry> backends)
    {
        Modules = Sort(modules);
        Optimizers = Sort(optimizers);
        Backends = Sort(backends);

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
        Backends.OrderBy(x => x.CanonicalAlias, StringComparer.Ordinal).ThenBy(x => x.TypeReference.TypeFullName, StringComparer.Ordinal).ToList();

    private static bool TryResolve(IReadOnlyDictionary<string, RuntimeComponentManifestEntry> map, string alias, out RuntimeComponentManifestEntry? entry)
    {
        if (string.IsNullOrWhiteSpace(alias))
            Thrower.Argument(nameof(alias), "Alias must not be empty.");

        return map.TryGetValue(alias.Trim(), out entry);
    }

    private static RuntimeComponentManifestEntry Create(RuntimeComponentKind kind, string canonicalAlias, IReadOnlyList<string> aliases, string assemblySimpleName, string typeFullName)
    {
        return new RuntimeComponentManifestEntry(kind, canonicalAlias, aliases, new RuntimeTypeReference(assemblySimpleName, typeFullName));
    }

    private static IReadOnlyList<RuntimeComponentManifestEntry> Sort(IEnumerable<RuntimeComponentManifestEntry> entries)
    {
        return entries
            .Select(Normalize)
            .OrderBy(x => x.CanonicalAlias, StringComparer.Ordinal)
            .ThenBy(x => x.TypeReference.TypeFullName, StringComparer.Ordinal)
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
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
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
