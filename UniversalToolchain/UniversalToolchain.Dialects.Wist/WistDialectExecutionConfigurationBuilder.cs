using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Converts resolved dialect composition into explicit Wist execution wiring.
/// </summary>
public sealed class WistDialectExecutionConfigurationBuilder
{
    public WistDialectExecutionConfiguration Build(DialectBuildPlan buildPlan, DialectRuntimeComposition runtimeComposition)
    {
        if (buildPlan == null)
        {
            Thrower.ArgumentNull(nameof(buildPlan));
        }

        if (runtimeComposition == null)
        {
            Thrower.ArgumentNull(nameof(runtimeComposition));
        }

        if (!runtimeComposition.IsResolved)
        {
            Thrower.Argument(nameof(runtimeComposition), "Runtime composition must be resolved before execution wiring is built.");
        }

        var frontendModules = runtimeComposition.OrderedModules
            .Where(x => x.IsFrontendModule)
            .Select(x => x.ImplementationType);
        var irModules = runtimeComposition.OrderedModules
            .Where(x => x.IsIrProcessingModule)
            .Select(x => x.ImplementationType);
        var optimizers = runtimeComposition.EnabledOptimizers
            .Select(x => x.ImplementationType);
        var allowedIntrinsics = buildPlan.IntrinsicDirectives
            .Where(x => x.Allowed)
            .Select(x => x.Name);
        var forbiddenIntrinsics = buildPlan.IntrinsicDirectives
            .Where(x => !x.Allowed)
            .Select(x => x.Name);

        return new WistDialectExecutionConfiguration(
            buildPlan.Name,
            frontendModules,
            irModules,
            optimizers,
            runtimeComposition.EnabledBackends.Select(x => x.BackendTarget),
            allowedIntrinsics,
            forbiddenIntrinsics);
    }
}
