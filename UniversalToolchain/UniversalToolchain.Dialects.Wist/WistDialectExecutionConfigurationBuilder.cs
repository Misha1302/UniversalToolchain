using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public sealed class WistDialectExecutionConfigurationBuilder
{
    private readonly DialectBackendRuntimeConfigurationBuilder _backendConfigurationBuilder;
    private readonly IRuntimeKnownBackendsProvider _knownBackendsProvider;
    private readonly SelectedRuntimeExecutionShapeBuilder _shapeBuilder;

    public WistDialectExecutionConfigurationBuilder(
        SelectedRuntimeExecutionShapeBuilder shapeBuilder,
        DialectBackendRuntimeConfigurationBuilder backendConfigurationBuilder,
        IRuntimeKnownBackendsProvider knownBackendsProvider)
    {
        shapeBuilder = shapeBuilder.ArgNotNull();

        backendConfigurationBuilder = backendConfigurationBuilder.ArgNotNull();

        knownBackendsProvider = knownBackendsProvider.ArgNotNull();

        _shapeBuilder = shapeBuilder;
        _backendConfigurationBuilder = backendConfigurationBuilder;
        _knownBackendsProvider = knownBackendsProvider;
    }

    public WistDialectExecutionConfiguration Build(DialectBuildPlan buildPlan, SelectedRuntimePlan selectedRuntimePlan)
    {
        buildPlan = buildPlan.ArgNotNull();

        selectedRuntimePlan = selectedRuntimePlan.ArgNotNull();

        var shape = _shapeBuilder.Build(buildPlan, selectedRuntimePlan);
        var backends = shape.BackendEntries
            .Select(x => _backendConfigurationBuilder.Build(x, buildPlan, selectedRuntimePlan))
            .ToList();

        var allOptimizers = backends
            .SelectMany(x => x.OptimizerTypes)
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();

        var knownBackends = _knownBackendsProvider.GetKnownBackends();

        return new WistDialectExecutionConfiguration(
            shape.DialectName,
            shape.FrontendModuleTypes,
            shape.IRModuleTypes,
            allOptimizers,
            backends,
            knownBackends);
    }
}
