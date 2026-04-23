using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public sealed class WistDialectExecutionConfigurationBuilder
{
    private readonly DialectBackendRuntimeConfigurationBuilder _backendConfigurationBuilder;
    private readonly SelectedRuntimeExecutionShapeBuilder _shapeBuilder;

    public WistDialectExecutionConfigurationBuilder(
        SelectedRuntimeExecutionShapeBuilder shapeBuilder,
        DialectBackendRuntimeConfigurationBuilder backendConfigurationBuilder)
    {
        shapeBuilder = shapeBuilder.ArgNotNull();

        backendConfigurationBuilder = backendConfigurationBuilder.ArgNotNull();

        _shapeBuilder = shapeBuilder;
        _backendConfigurationBuilder = backendConfigurationBuilder;
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

        var knownBackends = backends
            .Select(x => x.BackendDescriptor)
            .OrderBy(x => x.BackendId)
            .ThenBy(x => string.Join("|", x.Aliases), StringComparer.Ordinal)
            .ToList();

        return new WistDialectExecutionConfiguration(
            shape.DialectName,
            shape.FrontendModuleTypes,
            shape.IRModuleTypes,
            allOptimizers,
            backends,
            knownBackends);
    }
}
