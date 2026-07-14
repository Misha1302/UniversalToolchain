namespace UniversalToolchain.ModuleContracts;

public sealed class SelectedModuleContractTableProvider(
    ModuleContractEnforcementPolicy enforcementPolicy,
    ModuleContractSelectionBuilder selectionBuilder) : ISelectedModuleContractTableProvider
{
    private readonly ModuleContractSelectionBuilder _selectionBuilder = selectionBuilder.ArgNotNull();
    private readonly ModuleContractEnforcementPolicy _enforcementPolicy = enforcementPolicy.ArgNotNull();
    private readonly KnownCoreContractDescriptorProvider _coreDescriptorProvider = new();

    public ModuleContractSelectionReport? Build(
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IAirOptimizer> optimizers) =>
        Build(frontendModules, optimizers, []);

    public ModuleContractSelectionReport? Build(
        IReadOnlyList<IFrontendCoreModule> frontendModules,
        IReadOnlyList<IAirOptimizer> optimizers,
        IReadOnlyList<IBackendPipelineComponent> backendComponents)
    {
        frontendModules = frontendModules.ArgNotNull();
        optimizers = optimizers.ArgNotNull();
        backendComponents = backendComponents.ArgNotNull();

        var selectedComponents = frontendModules
            .Cast<object>()
            .Concat(optimizers)
            .ToArray();
        var providers = selectedComponents
            .OfType<IModuleContractDescriptorProvider>()
            .Concat(backendComponents
                .OfType<IModuleContractBackendPipelineComponent>()
                .SelectMany(static component => component.DescriptorProviders))
            .Concat([_coreDescriptorProvider])
            .ToArray();
        if (selectedComponents.Length == 0 && backendComponents.Count == 0)
            return null;

        var selectedModules = selectedComponents
            .SelectMany(ReadSelectedModuleIds)
            .Concat(backendComponents
                .OfType<IModuleContractBackendPipelineComponent>()
                .SelectMany(ReadSelectedBackendModuleIds))
            .Concat([
                KnownCoreModuleIds.CompilerFacts,
                KnownCoreModuleIds.BackendCapabilities
            ])
            .Distinct()
            .OrderBy(static id => id.Value, StringComparer.Ordinal)
            .ToArray();

        return _selectionBuilder.Build(selectedModules, providers, _enforcementPolicy);
    }

    internal static IReadOnlyList<ModuleId> ReadSelectedModuleIds(object component)
    {
        if (component is IModuleContractDescriptorProvider provider)
        {
            var descriptorModuleIds = provider
                .GetFacets()
                .Select(static facet => facet.ModuleId)
                .Distinct()
                .OrderBy(static id => id.Value, StringComparer.Ordinal)
                .ToArray();
            if (descriptorModuleIds.Length > 0)
                return descriptorModuleIds;
        }

        return [CreateLegacyModuleId(component.GetType())];
    }

    internal static IReadOnlyList<ModuleId> ReadSelectedBackendModuleIds(IModuleContractBackendPipelineComponent component)
    {
        component = component.ArgNotNull();

        var descriptorModuleIds = component.DescriptorProviders
            .SelectMany(static provider => provider.GetFacets())
            .Select(static facet => facet.ModuleId)
            .Distinct()
            .OrderBy(static id => id.Value, StringComparer.Ordinal)
            .ToArray();
        if (descriptorModuleIds.Length > 0)
            return descriptorModuleIds;

        return [CreateBackendModuleId(component.ComponentId)];
    }

    private static ModuleId CreateLegacyModuleId(Type type)
    {
        var typeName = type.FullName ?? type.Name;
        var normalized = new string(typeName
            .Select(static ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '.')
            .ToArray());
        while (normalized.Contains("..", StringComparison.Ordinal))
            normalized = normalized.Replace("..", ".", StringComparison.Ordinal);

        normalized = normalized.Trim('.');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "unknown";

        return new ModuleId($"legacy.clr.{normalized}");
    }

    private static ModuleId CreateBackendModuleId(string componentId)
    {
        var normalized = new string(componentId
            .Select(static ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '.')
            .ToArray());
        while (normalized.Contains("..", StringComparison.Ordinal))
            normalized = normalized.Replace("..", ".", StringComparison.Ordinal);

        normalized = normalized.Trim('.');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "unknown";

        if (normalized.StartsWith("backend.", StringComparison.Ordinal))
            return new ModuleId(normalized);

        return new ModuleId($"backend.{normalized}");
    }
}
