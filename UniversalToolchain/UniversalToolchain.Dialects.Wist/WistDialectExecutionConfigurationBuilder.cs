using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Converts resolved dialect composition into explicit Wist execution wiring.
/// </summary>
public sealed class WistDialectExecutionConfigurationBuilder
{
    public WistDialectExecutionConfiguration Build(DialectBuildPlan buildPlan, DialectRuntimeComposition runtimeComposition, DialectRuntimeDescriptorRegistry registry)
    {
        if (buildPlan == null)
            Thrower.ArgumentNull(nameof(buildPlan));

        if (runtimeComposition == null)
            Thrower.ArgumentNull(nameof(runtimeComposition));

        if (registry == null)
            Thrower.ArgumentNull(nameof(registry));

        if (!runtimeComposition.IsResolved)
            Thrower.Argument(nameof(runtimeComposition), "Runtime composition must be resolved before execution wiring is built.");

        var frontendModules = runtimeComposition.OrderedModules
            .Where(x => x.IsFrontendModule)
            .Select(x => x.ImplementationType);
        var irModules = runtimeComposition.OrderedModules
            .Where(x => x.IsIrProcessingModule)
            .Select(x => x.ImplementationType);
        var optimizers = runtimeComposition.EnabledOptimizers
            .Select(x => x.ImplementationType);
        var backends = runtimeComposition.EnabledBackends
            .Select(backend => BuildBackendConfiguration(backend, buildPlan, runtimeComposition))
            .ToList();

        return new WistDialectExecutionConfiguration(
            buildPlan.Name,
            frontendModules,
            irModules,
            optimizers,
            backends,
            registry.Backends.Values);
    }

    private static WistDialectBackendConfiguration BuildBackendConfiguration(
        RuntimeBackendDescriptor backend,
        DialectBuildPlan buildPlan,
        DialectRuntimeComposition runtimeComposition)
    {
        var allowedIntrinsics = runtimeComposition.AllowedIntrinsics
            .Where(x => x.AppliesTo(backend.BackendId))
            .Select(x => x.CanonicalId);
        var hasExplicitAllowList = buildPlan.IntrinsicDirectives.Any(x => x.Allowed && x.Target.Matches(backend.BackendId));
        var forbiddenIntrinsics = buildPlan.IntrinsicDirectives
            .Where(x => !x.Allowed && x.Target.Matches(backend.BackendId))
            .Select(x => x.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        return new WistDialectBackendConfiguration(backend, allowedIntrinsics, forbiddenIntrinsics, hasExplicitAllowList);
    }
}
