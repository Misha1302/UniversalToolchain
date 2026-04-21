using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DialectRuntimeSelectionExplanation
{
    private readonly ReadOnlyCollection<DialectDiagnostic> _diagnostics;
    private readonly ReadOnlyCollection<RuntimeComponentManifestEntry> _enabledBackends;
    private readonly ReadOnlyCollection<RuntimeComponentManifestEntry> _enabledOptimizers;
    private readonly ReadOnlyCollection<RuntimeComponentManifestEntry> _orderedModules;

    public DialectRuntimeSelectionExplanation(
        string selectionKind,
        bool isResolved,
        bool hasResolvedRuntimeComponents,
        IEnumerable<DialectDiagnostic> diagnostics,
        IEnumerable<RuntimeComponentManifestEntry> orderedModules,
        IEnumerable<RuntimeComponentManifestEntry> enabledOptimizers,
        IEnumerable<RuntimeComponentManifestEntry> enabledBackends)
    {
        if (string.IsNullOrWhiteSpace(selectionKind))
            Thrower.Argument(nameof(selectionKind), "Selection kind must not be empty.");

        SelectionKind = selectionKind;
        IsResolved = isResolved;
        HasResolvedRuntimeComponents = hasResolvedRuntimeComponents;
        _diagnostics = new ReadOnlyCollection<DialectDiagnostic>(Snapshot(diagnostics, nameof(diagnostics)));
        _orderedModules = new ReadOnlyCollection<RuntimeComponentManifestEntry>(Snapshot(orderedModules, nameof(orderedModules)));
        _enabledOptimizers = new ReadOnlyCollection<RuntimeComponentManifestEntry>(Snapshot(enabledOptimizers, nameof(enabledOptimizers)));
        _enabledBackends = new ReadOnlyCollection<RuntimeComponentManifestEntry>(Snapshot(enabledBackends, nameof(enabledBackends)));
    }

    public string SelectionKind { get; }

    public bool IsResolved { get; }

    public bool HasResolvedRuntimeComponents { get; }

    public IReadOnlyList<DialectDiagnostic> Diagnostics => _diagnostics;

    public IReadOnlyList<RuntimeComponentManifestEntry> OrderedModules => _orderedModules;

    public IReadOnlyList<RuntimeComponentManifestEntry> EnabledOptimizers => _enabledOptimizers;

    public IReadOnlyList<RuntimeComponentManifestEntry> EnabledBackends => _enabledBackends;

    private static List<T> Snapshot<T>(IEnumerable<T> source, [CallerArgumentExpression(nameof(source))] string? paramName = null)
    {
        source = source.ArgNotNull();
        return source.Select(item => item.NotNull()).ToList();
    }
}
