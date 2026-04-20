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
            var type = LoadComponentType(entry, RuntimeComponentKind.FrontendModule);
            var isFrontendModule = typeof(IFrontendCoreModule).IsAssignableFrom(type);
            var isIrModule = typeof(IIRProcessingModule).IsAssignableFrom(type);

            if (isFrontendModule && !frontendModules.Contains(type))
            {
                frontendModules.Add(type);
            }

            if (isIrModule)
            {
                irModules.Add(type);
            }

            if (!isFrontendModule && !isIrModule)
            {
                Thrower.InvalidOpEx(
                    $"Runtime module '{entry.CanonicalAlias}' resolves to type '{DisplayName(type)}', but the type does not implement IFrontendCoreModule or IIRProcessingModule.");
            }
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
            .Select(LoadOptimizerType)
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
        var policy = _intrinsicPolicyResolver.Resolve(buildPlan, backendId);
        var metadataOwnerType = LoadComponentType(backend, RuntimeComponentKind.Backend);
        return new DialectBackendRuntimeConfiguration(
            new RuntimeBackendDescriptor(backendId, metadataOwnerType, backend.Aliases),
            optimizerTypes,
            policy.Allowed,
            policy.Forbidden,
            policy.HasExplicitAllowList);
    }

    private Type LoadOptimizerType(RuntimeComponentManifestEntry entry)
    {
        var type = LoadComponentType(entry, RuntimeComponentKind.Optimizer);
        if (!typeof(IIRProcessingModule).IsAssignableFrom(type))
        {
            Thrower.InvalidOpEx(
                $"Runtime optimizer '{entry.CanonicalAlias}' resolves to type '{DisplayName(type)}', but the type does not implement IIRProcessingModule.");
        }

        return type;
    }

    private Type LoadComponentType(RuntimeComponentManifestEntry entry, RuntimeComponentKind expectedKind)
    {
        if (entry.Kind != expectedKind)
        {
            Thrower.InvalidOpEx(
                $"Runtime component '{entry.CanonicalAlias}' has kind '{RuntimeComponentKindCodec.Format(entry.Kind)}', but '{RuntimeComponentKindCodec.Format(expectedKind)}' was expected.");
        }

        return _typeLoader.LoadType(entry);
    }

    private static string DisplayName(Type type)
    {
        return type.FullName ?? type.Name;
    }
}
