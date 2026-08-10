using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk;

public sealed class LanguageCompiler
{
    private readonly LanguagePackageRegistry _registry;

    public LanguageCompiler(LanguagePackageRegistry registry) =>
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public LanguageBuildResult Compile(LanguageDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var diagnostics = new List<LanguageDiagnostic>();
        ValidateToolchainApi(definition, diagnostics);

        var featureStates = new Dictionary<LanguageFeatureId, VisitState>();
        var resolvedFeatures = new List<ResolvedLanguageFeature>();
        foreach (var selected in definition.SelectedFeatures.OrderBy(static x => x.Value, StringComparer.Ordinal))
            VisitFeature(selected, definition, featureStates, resolvedFeatures, diagnostics, []);
        ValidateFeatureCompatibility(definition, resolvedFeatures, diagnostics);
        if (HasErrors(diagnostics))
            return LanguageBuildResult.Failure(diagnostics);
        var activeFeatureIds = resolvedFeatures.Select(static item => item.Feature.Id).ToHashSet();

        var contributionStates = new Dictionary<LanguageContributionId, VisitState>();
        var resolvedContributions = new List<ResolvedLanguageContribution>();
        var seeds = resolvedFeatures.SelectMany(static x => x.Feature.Contributions)
            .Concat(definition.SlotOverrides.Select(static x => x.Contribution))
            .Distinct()
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .ToArray();
        foreach (var seed in seeds)
            VisitContribution(seed, definition, activeFeatureIds, contributionStates, resolvedContributions, diagnostics, []);
        foreach (var backend in definition.Backends)
            ResolveCapabilityProvider(LanguageCapabilities.Backend(backend), definition, activeFeatureIds, contributionStates, resolvedContributions, diagnostics, []);
        if (HasErrors(diagnostics))
            return LanguageBuildResult.Failure(diagnostics);

        (LanguagePackageDescriptor Package, LanguageContributionDescriptor Contribution, LanguagePackageRegistrationIdentity Identity)? runtimeProvider = null;
        if (definition.IsExecutable)
        {
            runtimeProvider = SelectRuntimeProvider(definition, activeFeatureIds, diagnostics);
            if (runtimeProvider == null || HasErrors(diagnostics))
                return LanguageBuildResult.Failure(diagnostics);
            VisitContribution(runtimeProvider.Value.Contribution.Id, definition, activeFeatureIds, contributionStates, resolvedContributions, diagnostics, []);
            if (HasErrors(diagnostics))
                return LanguageBuildResult.Failure(diagnostics);
        }

        var effectiveContributions = ApplySlotPolicies(definition, resolvedContributions, diagnostics);
        ValidateContributionCompatibility(effectiveContributions, diagnostics);
        ValidateContributionRequirements(effectiveContributions, diagnostics);
        if (HasErrors(diagnostics))
            return LanguageBuildResult.Failure(diagnostics);
        effectiveContributions = ApplyDefinitionContributionOrder(definition, effectiveContributions, diagnostics);
        if (HasErrors(diagnostics))
            return LanguageBuildResult.Failure(diagnostics);

        ResolvedLanguageContribution? effectiveRuntimeProvider = null;
        if (runtimeProvider != null)
        {
            effectiveRuntimeProvider = effectiveContributions.SingleOrDefault(
                x => x.Contribution.Id == runtimeProvider.Value.Contribution.Id);
            if (effectiveRuntimeProvider == null)
            {
                diagnostics.Add(Error(
                    "UTL2304",
                    "planning",
                    "The selected runtime-provider contribution was removed by a slot override.",
                    runtimeProvider.Value.Contribution.Id.Value,
                    "Select a runtime provider through UseRuntimeProvider instead of replacing its slot indirectly."));
                return LanguageBuildResult.Failure(diagnostics);
            }
        }

        var routes = effectiveRuntimeProvider == null
            ? []
            : BuildRoutes(definition, effectiveContributions, effectiveRuntimeProvider, diagnostics);
        if (HasErrors(diagnostics))
            return LanguageBuildResult.Failure(diagnostics);

        return LanguageBuildResult.Success(new LanguagePlan(
            definition,
            resolvedFeatures,
            effectiveContributions,
            effectiveRuntimeProvider,
            routes));
    }

    private static bool HasErrors(IEnumerable<LanguageDiagnostic> diagnostics) =>
        diagnostics.Any(static x => x.Severity == LanguageDiagnosticSeverity.Error);

    private static void ValidateToolchainApi(LanguageDefinition definition, ICollection<LanguageDiagnostic> diagnostics)
    {
        if (definition.ToolchainApiVersion != ToolchainApi.Current)
        {
            diagnostics.Add(Error(
                "UTL1501",
                "planning",
                $"Language targets Toolchain API {definition.ToolchainApiVersion.Major}, but this SDK supports {ToolchainApi.Current.Major}.",
                definition.Id.Value,
                "Target the installed Toolchain API or install a compatible SDK."));
        }
    }

    private void VisitFeature(
        LanguageFeatureId id,
        LanguageDefinition definition,
        Dictionary<LanguageFeatureId, VisitState> states,
        List<ResolvedLanguageFeature> output,
        List<LanguageDiagnostic> diagnostics,
        IReadOnlyList<LanguageFeatureId> chain)
    {
        if (states.TryGetValue(id, out var state))
        {
            if (state == VisitState.Visited)
                return;
            if (state == VisitState.Visiting)
            {
                diagnostics.Add(Error(
                    "UTL1003",
                    "planning",
                    $"Feature dependency cycle: {string.Join(" -> ", chain.Select(static x => x.Value).Append(id.Value))}.",
                    id.Value,
                    "Break the dependency cycle."));
            }
            return;
        }

        if (!_registry.TryGetFeatureRegistration(id, out var package, out var feature, out var packageIdentity))
        {
            diagnostics.Add(Error(
                "UTL1001",
                "planning",
                $"Required feature '{id.Value}' is not registered.",
                chain.Count == 0 ? null : chain[^1].Value,
                "Register a package that owns the missing feature."));
            return;
        }
        if (package.ToolchainApiVersion != definition.ToolchainApiVersion)
        {
            diagnostics.Add(Error(
                "UTL1501",
                "planning",
                $"Feature package '{package.Id.Value}' targets Toolchain API {package.ToolchainApiVersion.Major}, language targets {definition.ToolchainApiVersion.Major}.",
                package.Id.Value,
                "Use a feature package compatible with the language Toolchain API."));
            return;
        }

        states[id] = VisitState.Visiting;
        var nextChain = chain.Append(id).ToArray();
        foreach (var dependency in feature.Requires.OrderBy(static x => x.Value, StringComparer.Ordinal))
            VisitFeature(dependency, definition, states, output, diagnostics, nextChain);
        states[id] = VisitState.Visited;
        if (!output.Any(x => x.Feature.Id == id))
        {
            output.Add(new ResolvedLanguageFeature(packageIdentity, feature));
        }
    }

    private static void ValidateFeatureCompatibility(
        LanguageDefinition definition,
        IReadOnlyList<ResolvedLanguageFeature> features,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var resolvedIds = features.Select(static x => x.Feature.Id).ToHashSet();
        foreach (var item in features)
        {
            foreach (var conflict in item.Feature.Conflicts)
            {
                if (resolvedIds.Contains(conflict))
                {
                    diagnostics.Add(Error(
                        "UTL1002",
                        "planning",
                        $"Feature '{item.Feature.Id.Value}' conflicts with selected feature '{conflict.Value}'.",
                        item.PackageId.Value,
                        "Remove one of the conflicting features."));
                }
            }
            foreach (var backend in definition.Backends)
            {
                if (item.Feature.SupportedBackends.Count != 0 && !item.Feature.SupportedBackends.Contains(backend))
                {
                    diagnostics.Add(Error(
                        "UTL1203",
                        "planning",
                        $"Backend '{backend.Value}' is not supported by feature '{item.Feature.Id.Value}'.",
                        item.PackageId.Value,
                        "Select a supported backend or remove the feature."));
                }
            }
        }
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
            diagnostics.Add(Error(
                "UTL2301",
                "planning",
                definition.RuntimeProvider == null
                    ? "No registered runtime provider supports all selected backends."
                    : $"Runtime provider '{definition.RuntimeProvider.ProviderId.Value}' version '{definition.RuntimeProvider.Version.Value}' is not registered or does not support all selected backends.",
                definition.Id.Value,
                "Register a compatible runtime-provider contribution or select another provider."));
            return null;
        }
        diagnostics.Add(Error(
            "UTL2302",
            "planning",
            $"Runtime provider selection is ambiguous: {string.Join(", ", candidates.Select(static x => $"{x.Contribution.RuntimeProviderId!.Value.Value}@{x.Contribution.RuntimeProviderVersion!.Value.Value}"))}.",
            definition.Id.Value,
            "Use UseRuntimeProvider to select one provider explicitly."));
        return null;
    }

    private void VisitContribution(
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
            diagnostics.Add(Error(
                "UTL2006",
                "planning",
                $"Required contribution '{id.Value}' was explicitly excluded.",
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
                diagnostics.Add(Error(
                    "UTL2003",
                    "planning",
                    $"Contribution dependency cycle: {string.Join(" -> ", chain.Select(static x => x.Value).Append(id.Value))}.",
                    id.Value,
                    "Break the contribution dependency cycle."));
            }
            return;
        }
        if (!_registry.TryGetContributionRegistration(id, out var package, out var contribution, out var packageIdentity))
        {
            diagnostics.Add(Error(
                "UTL2001",
                "planning",
                $"Required contribution '{id.Value}' is not registered.",
                chain.Count == 0 ? null : chain[^1].Value,
                "Register a package that owns the missing contribution."));
            return;
        }
        if (!_registry.IsContributionEligible(id, activeFeatureIds))
        {
            var owners = _registry.GetContributionOwners(id);
            diagnostics.Add(Error(
                "UTL2014",
                "planning",
                $"Contribution '{id.Value}' belongs to unselected feature(s): {string.Join(", ", owners.Select(static owner => owner.Value))}.",
                package.Id.Value,
                "Select an owning feature or move package-level infrastructure outside feature ownership."));
            return;
        }
        if (package.ToolchainApiVersion != definition.ToolchainApiVersion)
        {
            diagnostics.Add(Error(
                "UTL1501",
                "planning",
                $"Contribution package '{package.Id.Value}' targets Toolchain API {package.ToolchainApiVersion.Major}, language targets {definition.ToolchainApiVersion.Major}.",
                package.Id.Value,
                "Use a contribution package compatible with the language Toolchain API."));
            return;
        }

        states[id] = VisitState.Visiting;
        var nextChain = chain.Append(id).ToArray();
        foreach (var dependency in contribution.RequiresContributions)
            VisitContribution(dependency, definition, activeFeatureIds, states, output, diagnostics, nextChain);
        foreach (var capability in contribution.RequiresCapabilities)
            ResolveCapabilityProvider(capability, definition, activeFeatureIds, states, output, diagnostics, nextChain);
        states[id] = VisitState.Visited;
        if (!output.Any(x => x.Contribution.Id == id))
        {
            output.Add(new ResolvedLanguageContribution(packageIdentity, contribution));
        }
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
                diagnostics.Add(Error(
                    "UTL2005",
                    "planning",
                    $"Preferred contribution '{preferred.Value}' does not provide capability '{capability.Value}'.",
                    preferred.Value,
                    "Select a registered provider for the capability."));
                return;
            }
            VisitContribution(preferred, definition, activeFeatureIds, states, output, diagnostics, chain);
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
            VisitContribution(providers[0], definition, activeFeatureIds, states, output, diagnostics, chain);
            return;
        }
        if (providers.Length == 0)
        {
            diagnostics.Add(Error(
                "UTL2004",
                "planning",
                $"No contribution provides required capability '{capability.Value}'.",
                chain.Count == 0 ? null : chain[^1].Value,
                "Register a provider contribution or remove the requirement."));
            return;
        }
        diagnostics.Add(Error(
            "UTL2002",
            "planning",
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
                    diagnostics.Add(Error(
                        "UTL2102",
                        "planning",
                        $"Slot override '{group.Key.Value}' selects contribution '{overrideValue.Contribution.Value}', but it is not available in the resolved graph.",
                        overrideValue.Contribution.Value,
                        "Register and select the replacement contribution."));
                    continue;
                }
                if (overrideValue.ExpectedCurrentOwner != null && ordered.All(x => x.Contribution.Id != overrideValue.ExpectedCurrentOwner.Value))
                {
                    diagnostics.Add(Error(
                        "UTL2103",
                        "planning",
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
                diagnostics.Add(Error(
                    "UTL2101",
                    "planning",
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

    private static void ValidateContributionCompatibility(
        IReadOnlyList<ResolvedLanguageContribution> contributions,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var ids = contributions.Select(static x => x.Contribution.Id).ToHashSet();
        var capabilities = contributions.SelectMany(static x => x.Contribution.ProvidesCapabilities).ToHashSet();
        foreach (var item in contributions)
        {
            foreach (var conflict in item.Contribution.Conflicts.Where(ids.Contains))
            {
                diagnostics.Add(Error(
                    "UTL2010",
                    "planning",
                    $"Contribution '{item.Contribution.Id.Value}' conflicts with '{conflict.Value}'.",
                    item.PackageId.Value,
                    "Remove one contribution or select a compatible implementation."));
            }
            foreach (var conflict in item.Contribution.ConflictsCapabilities.Where(capabilities.Contains))
            {
                diagnostics.Add(Error(
                    "UTL2011",
                    "planning",
                    $"Contribution '{item.Contribution.Id.Value}' conflicts with capability '{conflict.Value}'.",
                    item.PackageId.Value,
                    "Select a compatible contribution set."));
            }
        }
    }

    private static void ValidateContributionRequirements(
        IReadOnlyList<ResolvedLanguageContribution> contributions,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var ids = contributions.Select(static x => x.Contribution.Id).ToHashSet();
        var capabilities = contributions.SelectMany(static x => x.Contribution.ProvidesCapabilities).ToHashSet();
        foreach (var item in contributions)
        {
            foreach (var required in item.Contribution.RequiresContributions.Where(required => !ids.Contains(required)))
            {
                diagnostics.Add(Error(
                    "UTL2012",
                    "planning",
                    $"Contribution '{item.Contribution.Id.Value}' requires removed contribution '{required.Value}'.",
                    item.Contribution.Id.Value,
                    "Adjust the slot override or provide a compatible replacement."));
            }
            foreach (var required in item.Contribution.RequiresCapabilities.Where(required => !capabilities.Contains(required)))
            {
                diagnostics.Add(Error(
                    "UTL2013",
                    "planning",
                    $"Contribution '{item.Contribution.Id.Value}' requires missing capability '{required.Value}'.",
                    item.Contribution.Id.Value,
                    "Select a provider for the missing capability."));
            }
        }
    }

    private static IReadOnlyList<ResolvedLanguageContribution> ApplyDefinitionContributionOrder(
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
                diagnostics.Add(Error(
                    "UTL2110",
                    "planning",
                    $"Definition-level contribution order references an unselected contribution: '{constraint.Source.Value}' {constraint.Kind} '{constraint.Target.Value}'.",
                    constraint.Source.Value,
                    "Select both contributions or remove the ordering constraint."));
                continue;
            }
            if (source.Contribution.Slot != target.Contribution.Slot)
            {
                diagnostics.Add(Error(
                    "UTL2111",
                    "planning",
                    $"Definition-level contribution order cannot cross slots: '{constraint.Source.Value}' is in '{source.Contribution.Slot.Value}', while '{constraint.Target.Value}' is in '{target.Contribution.Slot.Value}'.",
                    constraint.Source.Value,
                    "Order contributions only within one language slot."));
            }
        }
        if (HasErrors(diagnostics))
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
                    diagnostics.Add(Error(
                        "UTL2112",
                        "planning",
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

    private static IReadOnlyList<LanguageArtifactRoute> BuildRoutes(
        LanguageDefinition definition,
        IReadOnlyList<ResolvedLanguageContribution> contributions,
        ResolvedLanguageContribution runtimeProvider,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var routes = new List<LanguageArtifactRoute>();
        foreach (var backend in definition.Backends.OrderBy(static x => x.Value, StringComparer.Ordinal))
        {
            var backendCapability = LanguageCapabilities.Backend(backend);
            var backendOwners = contributions
                .Where(item => item.Contribution.ProvidesCapabilities.Contains(backendCapability))
                .ToArray();
            if (backendOwners.Length != 1)
            {
                diagnostics.Add(Error(
                    "UTL2203",
                    "planning",
                    $"Backend '{backend.Value}' must have exactly one selected contribution owner, but {backendOwners.Length} were selected.",
                    backend.Value,
                    "Select one backend capability provider explicitly."));
                continue;
            }

            var backendOwner = backendOwners[0].Contribution;
            LanguageArtifactContract target;
            if (backendOwner.BackendInputContract is { } backendInputContract)
            {
                target = backendInputContract;
            }
            else if (!runtimeProvider.Contribution.RuntimeInputContracts.TryGetValue(backend, out target))
            {
                diagnostics.Add(Error(
                    "UTL2303",
                    "planning",
                    $"Neither backend contribution '{backendOwner.Id.Value}' nor runtime provider '{runtimeProvider.Contribution.RuntimeProviderId!.Value.Value}' declares an execution input artifact for backend '{backend.Value}'.",
                    backendOwner.Id.Value,
                    "Declare the backend execution input contract on the backend contribution."));
                continue;
            }

            var transformations = contributions
                .Where(item => item.Contribution.Transformation != null)
                .Where(item => item.Contribution.SupportedBackends.Count == 0 || item.Contribution.SupportedBackends.Contains(backend))
                .ToArray();
            var conversionEdges = transformations
                .Where(static item => !item.Contribution.Transformation!.IsPass)
                .Select(static item => new RouteEdge(item.Contribution.Id, item.Contribution.Transformation!))
                .ToArray();
            var baseSteps = FindBestRoute(definition.EntryArtifact, target, conversionEdges);
            if (baseSteps == null)
            {
                diagnostics.Add(Error(
                    "UTL2201",
                    "planning",
                    $"No type-compatible artifact route exists from '{definition.EntryArtifact}' to '{target}' for backend '{backend.Value}'.",
                    runtimeProvider.Contribution.Id.Value,
                    "Register compatible typed artifact-transformer contributions or correct their contracts."));
                continue;
            }

            var steps = InsertPasses(
                definition.EntryArtifact,
                baseSteps,
                transformations.Where(static item => item.Contribution.Transformation!.IsPass).ToArray(),
                backend,
                diagnostics);
            if (steps == null)
                continue;
            routes.Add(new LanguageArtifactRoute(backend, definition.EntryArtifact, target, steps));
        }
        return routes;
    }

    private static IReadOnlyList<LanguageArtifactRouteStep>? InsertPasses(
        LanguageArtifactContract source,
        IReadOnlyList<LanguageArtifactRouteStep> baseSteps,
        IReadOnlyList<ResolvedLanguageContribution> passes,
        BackendId backend,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var result = new List<LanguageArtifactRouteStep>();
        var remaining = passes.ToDictionary(static item => item.Contribution.Id);
        var current = source;

        foreach (var step in baseSteps)
        {
            if (!AppendPassesForContract(current, remaining, result, backend, diagnostics))
                return null;
            result.Add(step);
            current = step.TargetContract;
        }
        if (!AppendPassesForContract(current, remaining, result, backend, diagnostics))
            return null;
        if (remaining.Count != 0)
        {
            var unplaced = string.Join(", ", remaining.Values
                .OrderBy(static item => item.Contribution.Id.Value, StringComparer.Ordinal)
                .Select(static item => $"{item.Contribution.Id.Value} ({item.Contribution.Transformation!.SourceContract})"));
            diagnostics.Add(Error(
                "UTL2204",
                "planning",
                $"Selected artifact passes cannot be placed on the route for backend '{backend.Value}': {unplaced}.",
                backend.Value,
                "Remove the pass, restrict its supported backends, or provide a route containing its artifact contract."));
            return null;
        }
        return result;
    }

    private static bool AppendPassesForContract(
        LanguageArtifactContract contract,
        IDictionary<LanguageContributionId, ResolvedLanguageContribution> remaining,
        ICollection<LanguageArtifactRouteStep> output,
        BackendId backend,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        var candidates = remaining.Values
            .Where(item => item.Contribution.SupportedBackends.Count == 0 || item.Contribution.SupportedBackends.Contains(backend))
            .Where(item => LanguageArtifactRoute.ContractsConnect(contract, item.Contribution.Transformation!.SourceContract) &&
                           LanguageArtifactRoute.ContractsConnect(item.Contribution.Transformation.TargetContract, contract))
            .ToDictionary(static item => item.Contribution.Id);
        if (candidates.Count == 0)
            return true;

        var emitted = new HashSet<LanguageContributionId>();
        while (emitted.Count != candidates.Count)
        {
            var ready = candidates.Values
                .Where(item => !emitted.Contains(item.Contribution.Id))
                .Where(item => item.Contribution.AfterContributions
                    .Where(candidates.ContainsKey)
                    .All(emitted.Contains))
                .Where(item => candidates.Values
                    .Where(other => other.Contribution.BeforeContributions.Contains(item.Contribution.Id))
                    .All(other => emitted.Contains(other.Contribution.Id)))
                .OrderBy(static item => item.Contribution.Order)
                .ThenBy(static item => item.Contribution.Id.Value, StringComparer.Ordinal)
                .FirstOrDefault();
            if (ready == null)
            {
                diagnostics.Add(Error(
                    "UTL2202",
                    "planning",
                    $"Artifact passes for contract '{contract}' contain an ordering cycle.",
                    contract.Kind.Value,
                    "Remove the cyclic Before/After constraints."));
                return false;
            }

            var transformation = ready.Contribution.Transformation!;
            output.Add(new LanguageArtifactRouteStep(
                ready.Contribution.Id,
                transformation.SourceContract,
                transformation.TargetContract,
                transformation.Cost));
            emitted.Add(ready.Contribution.Id);
            remaining.Remove(ready.Contribution.Id);
        }
        return true;
    }

    private static IReadOnlyList<LanguageArtifactRouteStep>? FindBestRoute(
        LanguageArtifactContract source,
        LanguageArtifactContract target,
        IReadOnlyList<RouteEdge> edges)
    {
        var best = new Dictionary<LanguageArtifactContract, RouteState>
        {
            [source] = new RouteState(0, string.Empty, [])
        };
        var pending = new HashSet<LanguageArtifactContract> { source };
        while (pending.Count != 0)
        {
            var current = pending
                .OrderBy(node => best[node].Cost)
                .ThenBy(node => best[node].Signature, StringComparer.Ordinal)
                .ThenBy(static node => node.ToString(), StringComparer.Ordinal)
                .First();
            pending.Remove(current);
            var currentState = best[current];
            foreach (var edge in edges
                         .Where(edge => LanguageArtifactRoute.ContractsConnect(current, edge.Transformation.SourceContract))
                         .OrderBy(static edge => edge.ContributionId.Value, StringComparer.Ordinal))
            {
                var next = edge.Transformation.TargetContract;
                var signature = string.IsNullOrEmpty(currentState.Signature)
                    ? edge.ContributionId.Value
                    : currentState.Signature + "|" + edge.ContributionId.Value;
                var candidate = new RouteState(
                    currentState.Cost + edge.Transformation.Cost,
                    signature,
                    currentState.Steps.Append(new LanguageArtifactRouteStep(
                        edge.ContributionId,
                        edge.Transformation.SourceContract,
                        edge.Transformation.TargetContract,
                        edge.Transformation.Cost)).ToArray());
                if (!best.TryGetValue(next, out var existing) ||
                    candidate.Cost < existing.Cost ||
                    candidate.Cost == existing.Cost && StringComparer.Ordinal.Compare(candidate.Signature, existing.Signature) < 0)
                {
                    best[next] = candidate;
                    pending.Add(next);
                }
            }
        }

        return best
            .Where(pair => LanguageArtifactRoute.ContractsConnect(pair.Key, target))
            .OrderBy(static pair => pair.Value.Cost)
            .ThenBy(static pair => pair.Value.Signature, StringComparer.Ordinal)
            .Select(static pair => pair.Value.Steps)
            .FirstOrDefault();
    }

    private static LanguageDiagnostic Error(string code, string stage, string message, string? owner, string hint) =>
        new(code, LanguageDiagnosticSeverity.Error, stage, message, owner, hint);

    private sealed record RouteEdge(
        LanguageContributionId ContributionId,
        ArtifactTransformationDescriptor Transformation);

    private sealed record RouteState(
        int Cost,
        string Signature,
        IReadOnlyList<LanguageArtifactRouteStep> Steps);

    private enum VisitState
    {
        Visiting,
        Visited
    }
}
