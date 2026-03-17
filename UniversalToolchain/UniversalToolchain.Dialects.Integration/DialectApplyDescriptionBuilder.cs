using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Converts resolved runtime composition to explicit apply-mode description without mutating runtime services.
/// </summary>
public sealed class DialectApplyDescriptionBuilder
{
    public DialectApplyDescription Build(DialectRuntimeComposition runtimeComposition)
    {
        if (runtimeComposition == null)
            Thrower.ArgumentNull(nameof(runtimeComposition));

        if (!runtimeComposition.IsResolved)
            Thrower.Argument(nameof(runtimeComposition), "Cannot build apply description from unresolved runtime composition.");

        var frontendModules = new List<Type>();
        var irProcessingModules = new List<Type>();

        foreach (var descriptor in runtimeComposition.OrderedModules)
        {
            if (descriptor.IsFrontendModule)
                frontendModules.Add(descriptor.ImplementationType);

            if (descriptor.IsIrProcessingModule)
                irProcessingModules.Add(descriptor.ImplementationType);
        }

        var optimizers = runtimeComposition.EnabledOptimizers
            .Select(x => x.ImplementationType)
            .ToList();

        var runtimeBackends = runtimeComposition.EnabledBackends
            .Select(x => x.RuntimeName)
            .ToList();

        var intrinsics = runtimeComposition.AllowedIntrinsics
            .Select(x => new DialectApplyIntrinsicPermission(x.Name, x.Target))
            .ToList();

        return new DialectApplyDescription(
            runtimeComposition.DialectName,
            frontendModules,
            irProcessingModules,
            optimizers,
            runtimeBackends,
            intrinsics);
    }
}
