using System.Collections.ObjectModel;

namespace UniversalToolchain.Semantics.Abstractions;

public enum SemanticEffectKind
{
    Pure,
    ReadsRuntimeState,
    WritesRuntimeState,
    ReadsMemory,
    WritesMemory,
    Allocates,
    MayThrow,
    CallsExternalCode,
    ControlEffect,
    UnknownExternalEffect
}

public sealed class SemanticEffectSummary
{
    private readonly ReadOnlyCollection<SemanticEffectKind> _effects;

    public SemanticEffectSummary(IEnumerable<SemanticEffectKind>? effects = null)
    {
        _effects = new ReadOnlyCollection<SemanticEffectKind>((effects ?? [])
            .Distinct()
            .Order()
            .ToList());
    }

    public static SemanticEffectSummary Pure { get; } = new();

    public IReadOnlyList<SemanticEffectKind> Effects => _effects;

    public bool IsPure => _effects.Count == 0 || _effects.All(static x => x == SemanticEffectKind.Pure);

    public bool HasObservableEffects => !IsPure;

    public bool Contains(SemanticEffectKind effect) => _effects.Contains(effect);
}
