using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

public sealed class WistRuntimeManifest : IWistRuntimeManifest
{
    private static readonly IReadOnlyList<RuntimeComponentManifestEntry> ModuleEntries =
    [
        new(RuntimeComponentKind.FrontendModule, "Arithmetic", [], "ArithmeticModule", "ArithmeticModule.Module.ArithmeticModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "Comments", [], "CommentsModule", "CommentsModule.CommentsModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "Conditions", [], "ConditionsModule", "ConditionsModule.Module.ConditionsModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "ComparisonConditions", [], "ConditionsModule", "ConditionsModule.Enums.ComparisonOperations"),
        new(RuntimeComponentKind.FrontendModule, "BooleanConditions", [], "ConditionsModule", "ConditionsModule.Enums.BooleanOperations"),
        new(RuntimeComponentKind.FrontendModule, "CSharpInterop", [], "CSharpInteropModule", "CSharpInteropModule.Module.CSharpInteropModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "Equality", [], "EqualityModule", "EqualityModule.EqualityModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "Identifier", [], "IdentifierModule", "IdentifierModule.IdentifierModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "InternalPreprocessorLexemes", [], "InternalPreprocessorLexemesModule", "InternalPreprocessorLexemesModule.InternalPreprocessorLexemesModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "Labels", [], "LabelsModule", "LabelsModule.Module.LabelsModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "Loops", [], "LoopsModule", "LoopsModule.Module.LoopsModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "NativeTypes", [], "NativeMathModule", "NativeMathModule.NativeTypesModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "Numbers", [], "NumbersModule", "NumbersModule.Module.NumbersModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "ParametersSetter", [], "ParametersSetterModule", "ParametersSetterModule.ParametersSetterModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "Scopes", [], "ScopesModule", "ScopesModule.Module.ScopesModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "SemicolonAsNewLine", [], "SemicolonAsNewLineModule", "SemicolonAsNewLineModule.SemicolonAsNewLineModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "Variables", [], "VariablesModule", "VariablesModule.VariablesModuleImpl"),
        new(RuntimeComponentKind.FrontendModule, "Whitespaces", [], "WhitespacesModule", "WhitespacesModule.WhitespaceModuleImpl")
    ];

    private static readonly IReadOnlyList<RuntimeComponentManifestEntry> OptimizerEntries =
    [
        new(RuntimeComponentKind.Optimizer, "ArithmeticOptimization", [], "NativeMathModule", "NativeMathModule.ArithmeticOptimizerModule"),
        new(RuntimeComponentKind.Optimizer, "BooleanOptimization", [], "ConditionsModule", "ConditionsModule.Optimizers.BooleanOptimizerModule"),
        new(RuntimeComponentKind.Optimizer, "ComparisonIntrinsicOptimization", [], "ConditionsModule", "ConditionsModule.Optimizers.ComparisonIntrinsicOptimizerModule"),
        new(RuntimeComponentKind.Optimizer, "EGraphOptimization", [], "NativeMathModule", "NativeMathModule.EGraphOptimizerModule"),
        new(RuntimeComponentKind.Optimizer, "LocalVariablesOptimization", [], "LocalVariablesOptimizerModule", "LocalVariablesOptimizerModule.LocalVariablesOptimizer"),
        new(RuntimeComponentKind.Optimizer, "NativeCilOptimization", [], "NativeMathModule", "NativeMathModule.NativeCilOptimizerModule"),
        new(RuntimeComponentKind.Optimizer, "NativeTypesOptimization", [], "NativeMathModule", "NativeMathModule.NativeTypesOptimizerModule")
    ];

    private static readonly IReadOnlyList<RuntimeComponentManifestEntry> BackendEntries =
    [
        new(RuntimeComponentKind.Backend, "cil", ["compiler"], "UniversalToolchain.Dialects.Wist", "UniversalToolchain.Dialects.Wist.WistCilBackendDeclaration"),
        new(RuntimeComponentKind.Backend, "interpreter", [], "UniversalToolchain.Dialects.Wist", "UniversalToolchain.Dialects.Wist.WistInterpreterBackendDeclaration")
    ];

    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _backendsByAlias;
    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _modulesByAlias;
    private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _optimizersByAlias;

    public WistRuntimeManifest()
    {
        Modules = Sort(ModuleEntries);
        Optimizers = Sort(OptimizerEntries);
        Backends = Sort(BackendEntries);

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

    private static bool TryResolve(IReadOnlyDictionary<string, RuntimeComponentManifestEntry> map, string alias, out RuntimeComponentManifestEntry? entry)
    {
        if (string.IsNullOrWhiteSpace(alias))
            Thrower.Argument(nameof(alias), "Alias must not be empty.");

        return map.TryGetValue(alias, out entry);
    }

    private static IReadOnlyList<RuntimeComponentManifestEntry> Sort(IEnumerable<RuntimeComponentManifestEntry> entries)
    {
        return entries
            .Select(Normalize)
            .OrderBy(x => x.CanonicalAlias, StringComparer.Ordinal)
            .ThenBy(x => x.TypeFullName, StringComparer.Ordinal)
            .ToList();
    }

    private static RuntimeComponentManifestEntry Normalize(RuntimeComponentManifestEntry entry)
    {
        var canonical = entry.CanonicalAlias.NotNull(nameof(entry)).Trim();
        if (string.IsNullOrWhiteSpace(canonical))
            Thrower.Argument(nameof(entry), "Canonical alias must not be empty.");

        var aliases = (entry.Aliases ?? []).Select(x => x.NotNull(nameof(entry)).Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        return entry with { CanonicalAlias = canonical, Aliases = aliases };
    }

    private static IReadOnlyDictionary<string, RuntimeComponentManifestEntry> CreateAliasMap(IEnumerable<RuntimeComponentManifestEntry> entries, string parameterName)
    {
        var map = new Dictionary<string, RuntimeComponentManifestEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            foreach (var alias in entry.AllAliases)
            {
                if (!map.TryAdd(alias, entry))
                    Thrower.InvalidOpEx($"Duplicate runtime component alias '{alias}' in '{parameterName}'.");
            }
        }

        return map;
    }
}
