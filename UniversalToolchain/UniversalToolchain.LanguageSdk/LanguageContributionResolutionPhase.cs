using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk;

internal sealed record LanguageContributionResolutionResult(
    IReadOnlyList<ResolvedLanguageContribution> Contributions,
    ResolvedLanguageContribution? RuntimeProvider);

internal sealed class LanguageContributionResolutionPhase(LanguagePackageRegistry registry)
{
    private readonly LanguagePackageRegistry _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public LanguageContributionResolutionResult Resolve(
        LanguageDefinition definition,
        IReadOnlyList<ResolvedLanguageFeature> resolvedFeatures,
        List<LanguageDiagnostic> diagnostics)
    {
        var activeFeatureIds = resolvedFeatures.Select(static item => item.Feature.Id).ToHashSet();
        var states = new Dictionary<LanguageContributionId, VisitState>();
        var resolved = new List<ResolvedLanguageContribution>();
        var seeds = resolvedFeatures.SelectMany(static x => x.Feature.Contributions)
            .Concat(definition.SlotOverrides.Select(static x => x.Contribution))
            .Distinct()
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .ToArray();
        foreach (var seed in seeds)
            Visit(seed, definition, activeFeatureIds, states, resolved, diagnostics, []);
        foreach (var backend in definition.Backends)
            ResolveCapabilityProvider(LanguageCapabilities.Backend(backend), definition, activeFeatureIds, states, resolved, diagnostics, []);
        if (LanguagePlanningDiagnostics.HasErrors(diagnostics))
            return new LanguageContributionResolutionResult(resolved, null);

        (LanguagePackageDescriptor Package, LanguageContributionDescriptor Contribution, LanguagePackageRegistrationIdentity Identity)? runtimeProvider = null;
        if (definition.IsExecutable)
        {
            runtimeProvider = SelectRuntimeProvider(definition, activeFeatureIds, diagnostics);
            if (runtimeProvider == null || LanguagePlanningDiagnostics.HasErrors(diagnostics))
                return new LanguageContributionResolutionResult(resolved, null);
            Visit(runtimeProvider.Value.Contribution.Id, definition, activeFeatureIds, states, resolved, diagnostics, []);
            if (LanguagePlanningDiagnostics.HasErrors(diagnostics))
                return new LanguageContributionResolutionResult(resolved, null);
        }

        var effective = ApplySlotPolicies(definition, resolved, diagnostics);
        ValidateCompatibility(effective, diagnostics);
        ValidateRequirements(effective, diagnostics);
        if (LanguagePlanningDiagnostics.HasErrors(diagnostics))
            return new LanguageContributionResolutionResult(effective, null);
        effective = ApplyDefinitionOrder(definition, effective, diagnostics);
        if (LanguagePlanningDiagnostics.HasErrors(diagnostics))
            return new LanguageContributionResolutionResult(effective, null);

        ResolvedLanguageContribution? effectiveRuntimeProvider = null;
        if (runtimeProvider != null)
        {
            effectiveRuntimeProvider = effective.SingleOrDefault(
                x => x.Contribution.Id == runtimeProvider.Value.Contribution.Id);
            if (effectiveRuntimeProvider == null)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2304", "planning",
                    "The selected runtime-provider contribution was removed by a slot override.",
                    runtimeProvider.Value.Contribution.Id.Value,
                    "Select a runtime provider through UseRuntimeProvider instead of replacing its slot indirectly."));
            }
        }

        return new LanguageContributionResolutionResult(effective, effectiveRuntimeProvider);
    }

    private (LanguagePackageDescriptor Package, LanguageContributionDescriptor Contribution, LanguagePackageRegistrationIdentity Identity)? SelectRuntimeProvider(
        LanguageDefinition definition,
        IReadOnlySet<LanguageFeatureId> activeFeatureIds,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var candidates = _registry.GetRuntimeProviderRegistrations()
            .Where(x => !definition.ExcludedContributions.Contains(x.Contribution.Id))
            .Where(x => x.Package.ToolchainApiVersion == definition.ToolchainApiVersion)
            .Where(x => _registry.IsContributionEligible(x.Contribution.Id, activeFeatureIds))
            .Where(x => x.Contribution.RuntimeInputContracts.Count == 0 || definition.Backends.All(x.Contribution.RuntimeInputContracts.ContainsKey))
            .ToArray();
        if (definition.RuntimeProvider != null)
        {
            candidates = candidates.Where(x =>
                    x.Contribution.RuntimeProviderId == definition.RuntimeProvider.ProviderId &&
                    x.Contribution.RuntimeProviderVersion == definition.RuntimeProvider.Version)
                .ToArray();
        }
        if (candidates.Length == 1)
            return candidates[0];
        if (candidates.Length == 0)
        {
            diagnostics.Add(LanguagePlanningDiagnostics.Error(
                "UTL2301", "planning",
                definition.RuntimeProvider == null
                    ? "No registered runtime provider supports all selected backends."
                    : $"Runtime provider '{definition.RuntimeProvider.ProviderId.Value}' version '{definition.RuntimeProvider.Version.Value}' is not registered or does not support all selected backends.",
                definition.Id.Value,
                "Register a compatible runtime-provider contribution or select another provider."));
            return null;
        }
        diagnostics.Add(LanguagePlanningDiagnostics.Error(
            "UTL2302", "planning",
            $"Runtime provider selection is ambiguous: {string.Join(", ", candidates.Select(static x => $"{x.Contribution.RuntimeProviderId!.Value.Value}@{x.Contribution.RuntimeProviderVersion!.Value.Value}"))}.",
            definition.Id.Value,
            "Use UseRuntimeProvider to select one provider explicitly."));
        return null;
    }

    private void Visit(
        LanguageContributionId id,
        LanguageDefinition definition,
        IReadOnlySet<LanguageFeatureId> activeFeatureIds,
        Dictionary<LanguageContributionId, VisitState> states,
        List<ResolvedLanguageContribution> output,
        List<LanguageDiagnostic> diagnostics,
        IReadOnlyList<LanguageContributionId> chain)
    {
        if (definition.ExcludedContributions.Contains(id))
        {
            diagnostics.Add(LanguagePlanningDiagnostics.Error(
                "UTL2006", "planning", $"Required contribution '{id.Value}' was explicitly excluded.",
                chain.Count == 0 ? null : chain[^1].Value,
                "Remove the exclusion or select an alternative provider."));
            return;
        }
        if (states.TryGetValue(id, out var state))
        {
            if (state == VisitState.Visited)
                return;
            if (state == VisitState.Visiting)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2003", "planning",
                    $"Contribution dependency cycle: {string.Join(" -> ", chain.Select(static x => x.Value).Append(id.Value))}.",
                    id.Value, "Break the contribution dependency cycle."));
            }
            return;
        }
        if (!_registry.TryGetContributionRegistration(id, out var package, out var contribution, out var packageIdentity))
        {
            diagnostics.Add(LanguagePlanningDiagnostics.Error(
                "UTL2001", "planning", $"Required contribution '{id.Value}' is not registered.",
                chain.Count == 0 ? null : chain[^1].Value,
                "Register a package that owns the missing contribution."));
            return;
        }
        if (!_registry.IsContributionEligible(id, activeFeatureIds))
        {
            var owners = _registry.GetContributionOwners(id);
            diagnostics.Add(LanguagePlanningDiagnostics.Error(
                "UTL2014", "planning",
                $"Contribution '{id.Value}' belongs to unselected feature(s): {string.Join(", ", owners.Select(static owner => owner.Value))}.",
                package.Id.Value,
                "Select an owning feature or move package-level infrastructure outside feature ownership."));
            return;
        }
        if (package.ToolchainApiVersion != definition.ToolchainApiVersion)
        {
            diagnostics.Add(LanguagePlanningDiagnostics.Error(
                "UTL1501", "planning",
                $"Contribution package '{package.Id.Value}' targets Toolchain API {package.ToolchainApiVersion.Major}, language targets {definition.ToolchainApiVersion.Major}.",
                package.Id.Value,
                "Use a contribution package compatible with the language Toolchain API."));
            return;
        }

        states[id] = VisitState.Visiting;
        var nextChain = chain.Append(id).ToArray();
        foreach (var dependency in contribution.RequiresContributions)
            Visit(dependency, definition, activeFeatureIds, states, output, diagnostics, nextChain);
        foreach (var capability in contribution.RequiresCapabilities)
            ResolveCapabilityProvider(capability, definition, activeFeatureIds, states, output, diagnostics, nextChain);
        states[id] = VisitState.Visited;
        if (!output.Any(x => x.Contribution.Id == id))
            output.Add(new ResolvedLanguageContribution(packageIdentity, contribution));
    }

    private void ResolveCapabilityProvider(
        LanguageCapabilityId capability,
        LanguageDefinition definition,
        IReadOnlySet<LanguageFeatureId> activeFeatureIds,
        Dictionary<LanguageContributionId, VisitState> states,
        List<ResolvedLanguageContribution> output,
        List<LanguageDiagnostic> diagnostics,
        IReadOnlyList<LanguageContributionId> chain)
    {
        var providers = _registry.GetCapabilityProviders(capability)
            .Where(id => !definition.ExcludedContributions.Contains(id))
            .Where(id => _registry.IsContributionEligible(id, activeFeatureIds))
            .ToArray();
        if (definition.CapabilityProviders.TryGetValue(capability, out var preferred))
        {
            if (!providers.Contains(preferred))
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2005", "planning",
                    $"Preferred contribution '{preferred.Value}' does not provide capability '{capability.Value}'.",
                    preferred.Value,
                    "Select a registered provider for the capability."));
                return;
            }
            Visit(preferred, definition, activeFeatureIds, states, output, diagnostics, chain);
            return;
        }

        var alreadySelected = output
            .Where(x => x.Contribution.ProvidesCapabilities.Contains(capability))
            .Select(static x => x.Contribution.Id)
            .Distinct()
            .ToArray();
        if (alreadySelected.Length == 1)
            return;
        if (providers.Length == 1)
        {
            Visit(providers[0], definition, activeFeatureIds, states, output, diagnostics, chain);
            return;
        }
        if (providers.Length == 0)
        {
            diagnostics.Add(LanguagePlanningDiagnostics.Error(
                "UTL2004", "planning", $"No contribution provides required capability '{capability.Value}'.",
                chain.Count == 0 ? null : chain[^1].Value,
                "Register a provider contribution or remove the requirement."));
            return;
        }
        diagnostics.Add(LanguagePlanningDiagnostics.Error(
            "UTL2002", "planning",
            $"Capability '{capability.Value}' has multiple providers: {string.Join(", ", providers.Select(static x => x.Value))}.",
            chain.Count == 0 ? null : chain[^1].Value,
            "Use PreferCapabilityProvider to select one provider explicitly."));
    }

    private static IReadOnlyList<ResolvedLanguageContribution> ApplySlotPolicies(
        LanguageDefinition definition,
        IReadOnlyList<ResolvedLanguageContribution> contributions,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var result = contributions.ToList();
        foreach (var group in contributions.GroupBy(static x => x.Contribution.Slot))
        {
            var ordered = group.OrderBy(static x => x.Contribution.Order)
                .ThenBy(static x => x.Contribution.Id.Value, StringComparer.Ordinal)
                .ToArray();
            var overrideValue = definition.SlotOverrides.FirstOrDefault(x => x.Slot == group.Key);
            if (overrideValue != null)
            {
                var replacement = ordered.SingleOrDefault(x => x.Contribution.Id == overrideValue.Contribution);
                if (replacement == null)
                {
                    diagnostics.Add(LanguagePlanningDiagnostics.Error(
                        "UTL2102", "planning",
                        $"Slot override '{group.Key.Value}' selects contribution '{overrideValue.Contribution.Value}', but it is not available in the resolved graph.",
                        overrideValue.Contribution.Value,
                        "Register and select the replacement contribution."));
                    continue;
                }
                if (overrideValue.ExpectedCurrentOwner != null && ordered.All(x => x.Contribution.Id != overrideValue.ExpectedCurrentOwner.Value))
                {
                    diagnostics.Add(LanguagePlanningDiagnostics.Error(
                        "UTL2103", "planning",
                        $"Slot override expected current owner '{overrideValue.ExpectedCurrentOwner.Value.Value}', but that contribution is not present.",
                        overrideValue.Contribution.Value,
                        "Update the expected owner or inspect package drift."));
                    continue;
                }
                result.RemoveAll(x => x.Contribution.Slot == group.Key && x.Contribution.Id != replacement.Contribution.Id);
                continue;
            }

            if (ordered.Length <= 1)
                continue;
            var hasSingleOwner = ordered.Any(static x =>
                x.Contribution.Multiplicity == LanguageSlotMultiplicity.Single ||
                x.Contribution.MergePolicy is ContributionMergePolicy.RejectDuplicate or ContributionMergePolicy.Replace);
            if (hasSingleOwner)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2101", "planning",
                    $"Slot '{group.Key.Value}' has multiple owners: {string.Join(", ", ordered.Select(static x => x.Contribution.Id.Value))}.",
                    group.Key.Value,
                    "Use ReplaceSlot to select one owner explicitly."));
            }
        }
        return result
            .OrderBy(static x => x.Contribution.Slot.Value, StringComparer.Ordinal)
            .ThenBy(static x => x.Contribution.Order)
            .ThenBy(static x => x.Contribution.Id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateCompatibility(
        IReadOnlyList<ResolvedLanguageContribution> contributions,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var ids = contributions.Select(static x => x.Contribution.Id).ToHashSet();
        var capabilities = contributions.SelectMany(static x => x.Contribution.ProvidesCapabilities).ToHashSet();
        foreach (var item in contributions)
        {
            foreach (var conflict in item.Contribution.Conflicts.Where(ids.Contains))
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2010", "planning",
                    $"Contribution '{item.Contribution.Id.Value}' conflicts with '{conflict.Value}'.",
                    item.PackageId.Value,
                    "Remove one contribution or select a compatible implementation."));
            }
            foreach (var conflict in item.Contribution.ConflictsCapabilities.Where(capabilities.Contains))
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2011", "planning",
                    $"Contribution '{item.Contribution.Id.Value}' conflicts with capability '{conflict.Value}'.",
                    item.PackageId.Value,
                    "Select a compatible contribution set."));
            }
        }
    }

    private static void ValidateRequirements(
        IReadOnlyList<ResolvedLanguageContribution> contributions,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var ids = contributions.Select(static x => x.Contribution.Id).ToHashSet();
        var capabilities = contributions.SelectMany(static x => x.Contribution.ProvidesCapabilities).ToHashSet();
        foreach (var item in contributions)
        {
            foreach (var required in item.Contribution.RequiresContributions.Where(required => !ids.Contains(required)))
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2012", "planning",
                    $"Contribution '{item.Contribution.Id.Value}' requires removed contribution '{required.Value}'.",
                    item.Contribution.Id.Value,
                    "Adjust the slot override or provide a compatible replacement."));
            }
            foreach (var required in item.Contribution.RequiresCapabilities.Where(required => !capabilities.Contains(required)))
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2013", "planning",
                    $"Contribution '{item.Contribution.Id.Value}' requires missing capability '{required.Value}'.",
                    item.Contribution.Id.Value,
                    "Select a provider for the missing capability."));
            }
        }
    }

    private static IReadOnlyList<ResolvedLanguageContribution> ApplyDefinitionOrder(
        LanguageDefinition definition,
        IReadOnlyList<ResolvedLanguageContribution> contributions,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        if (definition.ContributionOrderConstraints.Count == 0)
            return contributions;

        var byId = contributions.ToDictionary(static item => item.Contribution.Id);
        foreach (var constraint in definition.ContributionOrderConstraints)
        {
            if (!byId.TryGetValue(constraint.Source, out var source) || !byId.TryGetValue(constraint.Target, out var target))
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2110", "planning",
                    $"Definition-level contribution order references an unselected contribution: '{constraint.Source.Value}' {constraint.Kind} '{constraint.Target.Value}'.",
                    constraint.Source.Value,
                    "Select both contributions or remove the ordering constraint."));
                continue;
            }
            if (source.Contribution.Slot != target.Contribution.Slot)
            {
                diagnostics.Add(LanguagePlanningDiagnostics.Error(
                    "UTL2111", "planning",
                    $"Definition-level contribution order cannot cross slots: '{constraint.Source.Value}' is in '{source.Contribution.Slot.Value}', while '{constraint.Target.Value}' is in '{target.Contribution.Slot.Value}'.",
                    constraint.Source.Value,
                    "Order contributions only within one language slot."));
            }
        }
        if (LanguagePlanningDiagnostics.HasErrors(diagnostics))
            return contributions;

        var result = new List<ResolvedLanguageContribution>(contributions.Count);
        foreach (var slotGroup in contributions
                     .GroupBy(static item => item.Contribution.Slot)
                     .OrderBy(static group => group.Key.Value, StringComparer.Ordinal))
        {
            var candidates = slotGroup.ToDictionary(static item => item.Contribution.Id);
            var predecessors = candidates.Keys.ToDictionary(static id => id, static _ => new HashSet<LanguageContributionId>());
            foreach (var constraint in definition.ContributionOrderConstraints.Where(constraint =>
                         candidates.ContainsKey(constraint.Source) && candidates.ContainsKey(constraint.Target)))
            {
                if (constraint.Kind == LanguageContributionOrderKind.Before)
                    predecessors[constraint.Target].Add(constraint.Source);
                else
                    predecessors[constraint.Source].Add(constraint.Target);
            }

            var emitted = new HashSet<LanguageContributionId>();
            while (emitted.Count != candidates.Count)
            {
                var ready = candidates.Values
                    .Where(item => !emitted.Contains(item.Contribution.Id))
                    .Where(item => predecessors[item.Contribution.Id].All(emitted.Contains))
                    .OrderBy(static item => item.Contribution.Order)
                    .ThenBy(static item => item.Contribution.Id.Value, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (ready == null)
                {
                    diagnostics.Add(LanguagePlanningDiagnostics.Error(
                        "UTL2112", "planning",
                        $"Definition-level contribution order for slot '{slotGroup.Key.Value}' contains a cycle.",
                        slotGroup.Key.Value,
                        "Remove the cyclic Requires/Before/After constraints."));
                    return contributions;
                }

                result.Add(ready);
                emitted.Add(ready.Contribution.Id);
            }
        }
        return result;
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }
}
