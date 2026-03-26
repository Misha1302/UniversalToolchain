using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public sealed class DialectRuntimeProviderFactory
{
    private readonly IReadOnlyDictionary<DialectBackendId, IWistDialectBackendServiceProvider> _backendProviders;

    public DialectRuntimeProviderFactory(IEnumerable<IWistDialectBackendServiceProvider> backendProviders)
    {
        if (backendProviders == null)
            Thrower.ArgumentNull(nameof(backendProviders));

        _backendProviders = backendProviders.ToDictionary(x => x.BackendId, x => x);
    }

    public IServiceProvider CreateProvider(DialectResolvedRuntimeSelection selection, DialectBuildPlan buildPlan)
    {
        var services = new ServiceCollection();
        services.AddSelectedWistRuntimeServices(selection, _backendProviders.Values, buildPlan);
        return services.BuildServiceProvider(validateScopes: true);
    }

    public WistDialectExecutionConfiguration CreateConfiguration(DialectResolvedRuntimeSelection selection, DialectBuildPlan buildPlan)
    {
        var backendConfigurations = selection.EnabledBackends.Select(backend =>
        {
            var allowedIntrinsics = BuildAllowedIntrinsics(buildPlan, backend.CanonicalId);
            var forbiddenIntrinsics = buildPlan.IntrinsicDirectives
                .Where(x => !x.Allowed && x.Target.Matches(backend.CanonicalId))
                .Select(x => x.Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
            var hasExplicitAllowList = buildPlan.IntrinsicDirectives.Any(x => x.Allowed && x.Target.Matches(backend.CanonicalId));
            var runtimeDescriptor = new RuntimeBackendDescriptor(backend.CanonicalId, backend.ImplementationType, backend.Aliases);
            return new WistDialectBackendConfiguration(runtimeDescriptor, allowedIntrinsics, forbiddenIntrinsics, hasExplicitAllowList);
        }).ToList();

        var knownBackends = new List<RuntimeBackendDescriptor>
        {
            new(WistDialectBackendIds.Cil, typeof(WistCilBackendDeclaration), ["compiler", "cil"]),
            new(WistDialectBackendIds.Interpreter, typeof(WistInterpreterBackendDeclaration), ["interpreter"])
        };

        return new WistDialectExecutionConfiguration(
            buildPlan.Name,
            selection.OrderedModules.Where(x => x.IsFrontendModule).Select(x => x.ImplementationType),
            selection.OrderedModules.Where(x => x.IsIrProcessingModule).Select(x => x.ImplementationType),
            selection.EnabledOptimizers.Select(x => x.ImplementationType),
            backendConfigurations,
            knownBackends);
    }

    private List<string> BuildAllowedIntrinsics(DialectBuildPlan buildPlan, DialectBackendId backendId)
    {
        if (!_backendProviders.TryGetValue(backendId, out var backendProvider))
            Thrower.InvalidOpEx<List<string>>($"No backend provider was registered for '{backendId.Value}'.");

        var hasAnyAllowRule = buildPlan.IntrinsicDirectives.Any(x => x.Allowed && x.Target.Matches(backendId));
        if (!hasAnyAllowRule)
            return backendProvider.SupportedIntrinsics.ToList();

        var allowed = buildPlan.IntrinsicDirectives
            .Where(x => x.Allowed && x.Target.Matches(backendId))
            .Select(x => x.Name)
            .Distinct(StringComparer.Ordinal)
            .Where(x => backendProvider.SupportedIntrinsics.Contains(x, StringComparer.Ordinal))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        return allowed;
    }
}
