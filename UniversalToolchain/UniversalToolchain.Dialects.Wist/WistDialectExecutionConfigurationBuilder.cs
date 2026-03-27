using BasicCore.Contracts;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public sealed class WistDialectExecutionConfigurationBuilder
{
    private readonly DialectIntrinsicPolicyResolver _intrinsicPolicyResolver;
    private readonly IWistRuntimeManifest _manifest;
    private readonly IRuntimeComponentTypeLoader _typeLoader;

    public WistDialectExecutionConfigurationBuilder(
        IRuntimeComponentTypeLoader typeLoader,
        DialectIntrinsicPolicyResolver intrinsicPolicyResolver,
        IWistRuntimeManifest manifest)
    {
        _typeLoader = typeLoader ?? throw new ArgumentNullException(nameof(typeLoader));
        _intrinsicPolicyResolver = intrinsicPolicyResolver ?? throw new ArgumentNullException(nameof(intrinsicPolicyResolver));
        _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    }

    public WistDialectExecutionConfiguration Build(DialectBuildPlan buildPlan, SelectedRuntimePlan selectedRuntimePlan)
    {
        if (buildPlan == null)
            Thrower.ArgumentNull(nameof(buildPlan));

        if (selectedRuntimePlan == null)
            Thrower.ArgumentNull(nameof(selectedRuntimePlan));

        if (!selectedRuntimePlan.IsResolved)
            Thrower.Argument(nameof(selectedRuntimePlan), "Selected runtime plan must be resolved before execution wiring is built.");

        var frontendModules = new List<Type>();
        var irModules = new List<Type>();

        foreach (var entry in selectedRuntimePlan.OrderedModules)
        {
            var type = _typeLoader.LoadType(entry);
            if (typeof(IFrontendCoreModule).IsAssignableFrom(type))
                frontendModules.Add(type);

            if (typeof(IIRProcessingModule).IsAssignableFrom(type))
                irModules.Add(type);
        }

        var optimizers = selectedRuntimePlan.EnabledOptimizers
            .Select(_typeLoader.LoadType)
            .ToList();

        var backends = selectedRuntimePlan.EnabledBackends
            .Select(x => BuildBackendConfiguration(x, buildPlan))
            .ToList();

        var knownBackends = _manifest.Backends
            .Select(x => new RuntimeBackendDescriptor(new DialectBackendId(x.CanonicalAlias), x.Aliases))
            .ToList();

        return new WistDialectExecutionConfiguration(
            buildPlan.Name,
            frontendModules,
            irModules,
            optimizers,
            backends,
            knownBackends);
    }

    private WistDialectBackendConfiguration BuildBackendConfiguration(RuntimeComponentManifestEntry backend, DialectBuildPlan buildPlan)
    {
        var backendId = new DialectBackendId(backend.CanonicalAlias);
        var policy = _intrinsicPolicyResolver.Resolve(buildPlan, backendId);
        return new WistDialectBackendConfiguration(
            new RuntimeBackendDescriptor(backendId, backend.Aliases),
            policy.Allowed,
            policy.Forbidden,
            policy.HasExplicitAllowList);
    }
}
