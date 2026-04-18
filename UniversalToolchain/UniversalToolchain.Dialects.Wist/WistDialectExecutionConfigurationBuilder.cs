using BasicCore.Contracts;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public sealed class WistDialectExecutionConfigurationBuilder
{
    private readonly DialectIntrinsicPolicyResolver _intrinsicPolicyResolver;
    private readonly IRuntimeKnownBackendsProvider _knownBackendsProvider;
    private readonly IRuntimeComponentTypeLoader _typeLoader;

    public WistDialectExecutionConfigurationBuilder(
        IRuntimeComponentTypeLoader typeLoader,
        DialectIntrinsicPolicyResolver intrinsicPolicyResolver,
        IRuntimeKnownBackendsProvider knownBackendsProvider)
    {
        typeLoader = typeLoader.ArgNotNull();

        intrinsicPolicyResolver = intrinsicPolicyResolver.ArgNotNull();

        knownBackendsProvider = knownBackendsProvider.ArgNotNull();

        _typeLoader = typeLoader;
        _intrinsicPolicyResolver = intrinsicPolicyResolver;
        _knownBackendsProvider = knownBackendsProvider;
    }

    public WistDialectExecutionConfiguration Build(DialectBuildPlan buildPlan, SelectedRuntimePlan selectedRuntimePlan)
    {
        buildPlan = buildPlan.ArgNotNull();

        selectedRuntimePlan = selectedRuntimePlan.ArgNotNull();

        if (!selectedRuntimePlan.IsResolved)
            Thrower.Argument(nameof(selectedRuntimePlan), "Selected runtime plan must be resolved before execution wiring is built.");

        var frontendModules = new List<Type>
        {
            typeof(ProgramStructureFrontendModule)
        };
        var irModules = new List<Type>();

        foreach (var entry in selectedRuntimePlan.OrderedModules)
        {
            var type = _typeLoader.LoadType(entry);
            if (typeof(IFrontendCoreModule).IsAssignableFrom(type) && !frontendModules.Contains(type))
                frontendModules.Add(type);

            if (typeof(IIRProcessingModule).IsAssignableFrom(type))
                irModules.Add(type);
        }


        var backends = selectedRuntimePlan.EnabledBackends
            .Select(x => BuildBackendConfiguration(x, buildPlan, selectedRuntimePlan))
            .ToList();

        var allOptimizers = backends
            .SelectMany(x => x.OptimizerTypes)
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();

        var knownBackends = _knownBackendsProvider.GetKnownBackends();

        return new WistDialectExecutionConfiguration(
            buildPlan.Name,
            frontendModules,
            irModules,
            allOptimizers,
            backends,
            knownBackends);
    }

    private DialectBackendRuntimeConfiguration BuildBackendConfiguration(
        RuntimeComponentManifestEntry backend,
        DialectBuildPlan buildPlan,
        SelectedRuntimePlan selectedRuntimePlan)
    {
        var backendId = new DialectBackendId(backend.CanonicalAlias);
        var optimizerTypes = selectedRuntimePlan.EnabledOptimizers
            .Where(entry => buildPlan.OptimizerDirectives
                .Where(x => x.Enabled)
                .Any(x => string.Equals(x.Name, entry.CanonicalAlias, StringComparison.Ordinal)
                          && x.Target.Matches(backendId)))
            .Select(_typeLoader.LoadType)
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
        var policy = _intrinsicPolicyResolver.Resolve(buildPlan, backendId);
        var metadataOwnerType = _typeLoader.LoadType(backend);
        return new DialectBackendRuntimeConfiguration(
            new RuntimeBackendDescriptor(backendId, metadataOwnerType, backend.Aliases),
            optimizerTypes,
            policy.Allowed,
            policy.Forbidden,
            policy.HasExplicitAllowList);
    }
}