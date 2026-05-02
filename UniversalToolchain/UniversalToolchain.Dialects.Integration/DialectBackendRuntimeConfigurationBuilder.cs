using BasicCore.Contracts;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Builds backend-specific runtime configuration from selected runtime metadata.
/// </summary>
public sealed class DialectBackendRuntimeConfigurationBuilder
{
    private readonly IDialectBackendIntrinsicPolicyResolver _intrinsicPolicyResolver;
    private readonly IRuntimeComponentTypeLoader _typeLoader;

    public DialectBackendRuntimeConfigurationBuilder(
        IRuntimeComponentTypeLoader typeLoader,
        IDialectBackendIntrinsicPolicyResolver intrinsicPolicyResolver)
    {
        _typeLoader = typeLoader.ArgNotNull();
        _intrinsicPolicyResolver = intrinsicPolicyResolver.ArgNotNull();
    }

    public DialectBackendRuntimeConfiguration Build(
        RuntimeComponentManifestEntry backend,
        DialectBuildPlan buildPlan,
        SelectedRuntimePlan selectedRuntimePlan)
    {
        backend = backend.ArgNotNull();
        buildPlan = buildPlan.ArgNotNull();
        selectedRuntimePlan = selectedRuntimePlan.ArgNotNull();

        if (backend.Kind != RuntimeComponentKind.Backend)
            Thrower.InvalidOpEx(
                $"Runtime component '{backend.CanonicalAlias}' has kind '{RuntimeComponentKindCodec.Format(backend.Kind)}', but '{RuntimeComponentKindCodec.Format(RuntimeComponentKind.Backend)}' was expected.");

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
        var metadataOwnerType = _typeLoader.LoadType(backend);

        return new DialectBackendRuntimeConfiguration(
            backend,
            new RuntimeBackendDescriptor(backendId, metadataOwnerType, backend.Aliases),
            optimizerTypes,
            policy.Allowed,
            policy.Forbidden,
            policy.HasExplicitAllowList);
    }

    private Type LoadOptimizerType(RuntimeComponentManifestEntry entry)
    {
        if (entry.Kind != RuntimeComponentKind.Optimizer)
            Thrower.InvalidOpEx(
                $"Runtime component '{entry.CanonicalAlias}' has kind '{RuntimeComponentKindCodec.Format(entry.Kind)}', but '{RuntimeComponentKindCodec.Format(RuntimeComponentKind.Optimizer)}' was expected.");

        var type = _typeLoader.LoadType(entry);
        if (!typeof(IIRProcessingModule).IsAssignableFrom(type))
            Thrower.InvalidOpEx(
                $"Runtime optimizer '{entry.CanonicalAlias}' resolves to type '{DisplayName(type)}', but the type does not implement IIRProcessingModule.");

        return type;
    }

    private static string DisplayName(Type type) => type.FullName ?? type.Name;
}