using System.Collections.ObjectModel;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.FeatureSdk;

/// <summary>
/// Deterministic, fail-closed index of module descriptors.
/// <para>
/// The registry answers exactly one question — "which module describes this phase of this
/// contribution" — and refuses to answer it ambiguously. It is built from explicitly supplied
/// descriptors, never from assembly scanning, so the answer is stable across processes and
/// registration order. Resolution questions ("should this contribution be selected?") are
/// deliberately not expressible here.
/// </para>
/// </summary>
public sealed class LanguageModuleRegistry
{
    private readonly Dictionary<LanguageModuleId, LanguageModuleDescriptor> _modules = [];
    private readonly Dictionary<(LanguageModulePhase Phase, LanguageContributionId Contribution), LanguageModuleId> _owners = [];

    public LanguageModuleRegistry Add(LanguageModuleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return AddRange([descriptor]);
    }

    public LanguageModuleRegistry AddRange(IEnumerable<LanguageModuleDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        var batch = descriptors.ToList();
        if (batch.Any(static descriptor => descriptor is null))
            throw new ArgumentException("A module registry must not receive null descriptors.", nameof(descriptors));

        // Validate the whole batch before mutating, so a rejected batch leaves the registry unchanged.
        var seenIds = new HashSet<LanguageModuleId>();
        var seenOwners = new Dictionary<(LanguageModulePhase Phase, LanguageContributionId Contribution), LanguageModuleId>();
        foreach (var descriptor in batch)
        {
            if (_modules.ContainsKey(descriptor.Id) || !seenIds.Add(descriptor.Id))
                throw new InvalidOperationException($"Module '{descriptor.Id.Value}' is already registered.");

            foreach (var phase in descriptor.Phases)
            {
                foreach (var contribution in descriptor.Contributions)
                {
                    var key = (phase, contribution);
                    if (_owners.TryGetValue(key, out var existingOwner) && existingOwner != descriptor.Id)
                    {
                        throw new InvalidOperationException(
                            $"Phase '{phase}' of contribution '{contribution.Value}' is already owned by module " +
                            $"'{existingOwner.Value}'; module '{descriptor.Id.Value}' cannot claim it as well. Phase " +
                            "ownership must have exactly one owner, because a tie-break here would silently pick a " +
                            "lowering strategy.");
                    }

                    if (seenOwners.TryGetValue(key, out var batchOwner) && batchOwner != descriptor.Id)
                    {
                        throw new InvalidOperationException(
                            $"Phase '{phase}' of contribution '{contribution.Value}' is claimed by both module " +
                            $"'{batchOwner.Value}' and module '{descriptor.Id.Value}' in the same batch.");
                    }

                    seenOwners[key] = descriptor.Id;
                }
            }
        }

        foreach (var descriptor in batch)
        {
            _modules.Add(descriptor.Id, descriptor);
            foreach (var phase in descriptor.Phases)
            {
                foreach (var contribution in descriptor.Contributions)
                    _owners[(phase, contribution)] = descriptor.Id;
            }
        }

        return this;
    }

    /// <summary>
    /// Modules in canonical identifier order. Ordering is part of the contract, so callers never
    /// observe registration order.
    /// </summary>
    public IReadOnlyList<LanguageModuleDescriptor> Modules =>
        new ReadOnlyCollection<LanguageModuleDescriptor>(
            _modules.Values.OrderBy(static module => module.Id.Value, StringComparer.Ordinal).ToList());

    /// <summary>
    /// Returns the single module that owns <paramref name="phase"/> of <paramref name="contribution"/>.
    /// </summary>
    public bool TryGetOwner(
        LanguageModulePhase phase,
        LanguageContributionId contribution,
        out LanguageModuleDescriptor? descriptor)
    {
        if (_owners.TryGetValue((phase, contribution), out var moduleId))
        {
            descriptor = _modules[moduleId];
            return true;
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    /// Every contribution owned in at least one phase, in canonical order.
    /// </summary>
    public IReadOnlyList<LanguageContributionId> OwnedContributions =>
        new ReadOnlyCollection<LanguageContributionId>(
            _owners.Keys
                .Select(static key => key.Contribution)
                .Distinct()
                .OrderBy(static contribution => contribution.Value, StringComparer.Ordinal)
                .ToList());
}
