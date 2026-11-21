using System.Collections.ObjectModel;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.FeatureSdk;

/// <summary>
/// The compilation phase a module can take responsibility for.
/// <para>
/// The phases are ordered roughly along the artifact route, but the order is descriptive, not a
/// scheduling instruction: the planner derives order from contribution ordering and dependency
/// constraints, never from this value.
/// </para>
/// </summary>
public enum LanguageModulePhase
{
    Frontend = 0,
    Binding = 1,
    Lowering = 2,
    Optimization = 3,
    Emission = 4
}

/// <summary>
/// Descriptive identity for a group of contributions that one mechanism owns across the pipeline.
/// <para>
/// This is deliberately <em>not</em> a planner node. It never selects, adds, removes or reorders a
/// contribution; <see cref="UniversalToolchain.LanguageSdk.LanguageCompiler"/> remains the only
/// place where such decisions happen. What the descriptor provides is the vocabulary for two
/// questions the current hardcoded tables answer by contribution-id switch:
/// </para>
/// <list type="bullet">
/// <item>which module owns the transition for a given phase of a given contribution (lowering dispatch);</item>
/// <item>how selected contributions group into a mechanism for diagnostics and explain-plan.</item>
/// </list>
/// <para>
/// Because descriptor declarations are optional and ignored by resolution, a package that declares
/// none behaves exactly as before and adding them cannot change a plan hash.
/// </para>
/// </summary>
public sealed class LanguageModuleDescriptor
{
    public LanguageModuleDescriptor(
        LanguageModuleId id,
        IEnumerable<LanguageModulePhase> phases,
        IEnumerable<LanguageContributionId> contributions,
        IEnumerable<LanguageModuleId>? requiresModules = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Id = id;
        Phases = SnapshotPhases(phases);
        if (Phases.Count == 0)
            throw new ArgumentException("A module must declare at least one phase.", nameof(phases));
        foreach (var phase in Phases)
        {
            if (!Enum.IsDefined(phase))
                throw new ArgumentOutOfRangeException(nameof(phases), $"Module phase '{phase}' is not a defined value.");
        }

        Contributions = Snapshot(contributions, nameof(contributions));
        if (Contributions.Count == 0)
            throw new ArgumentException("A module must own at least one contribution.", nameof(contributions));

        RequiresModules = Snapshot(requiresModules, nameof(requiresModules));
        if (RequiresModules.Contains(id))
            throw new ArgumentException("A module cannot require itself.", nameof(requiresModules));
        Metadata = SnapshotDictionary(metadata);
    }

    /// <summary>Module identity. Unique within a registry.</summary>
    public LanguageModuleId Id { get; }

    /// <summary>
    /// Phases this module claims for its contributions, in declaration order. Descriptive only —
    /// never a scheduling input.
    /// </summary>
    public IReadOnlyList<LanguageModulePhase> Phases { get; }

    /// <summary>Contributions this module groups, in canonical identifier order.</summary>
    public IReadOnlyList<LanguageContributionId> Contributions { get; }

    /// <summary>Other modules that must be present for this one to make sense. Descriptive only.</summary>
    public IReadOnlyList<LanguageModuleId> RequiresModules { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T>? values, string paramName) where T : notnull
    {
        if (values is null)
            return new ReadOnlyCollection<T>([]);
        var items = values.ToList();
        if (items.Any(static item => item is null))
            throw new ArgumentException("A module declaration must not contain null entries.", paramName);
        return new ReadOnlyCollection<T>(items.Distinct().OrderBy(static item => item.ToString(), StringComparer.Ordinal).ToList());
    }

    /// <summary>
    /// Phases keep their declared order rather than identifier order: the declaration reads as the
    /// pipeline the module spans, and sorting an enum by its name would scramble that reading.
    /// </summary>
    private static IReadOnlyList<LanguageModulePhase> SnapshotPhases(IEnumerable<LanguageModulePhase>? phases)
    {
        if (phases is null)
            return new ReadOnlyCollection<LanguageModulePhase>([]);
        return new ReadOnlyCollection<LanguageModulePhase>(phases.Distinct().ToArray());
    }

    private static IReadOnlyDictionary<string, string> SnapshotDictionary(IReadOnlyDictionary<string, string>? values)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in values ?? new Dictionary<string, string>())
            result.Add(pair.Key, pair.Value);
        return new ReadOnlyDictionary<string, string>(result);
    }
}
