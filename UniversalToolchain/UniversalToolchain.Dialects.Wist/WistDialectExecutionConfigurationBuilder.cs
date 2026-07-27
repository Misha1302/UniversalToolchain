using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

internal sealed class WistDialectExecutionConfigurationBuilder
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

        var allOptimizers = DeduplicateStable(backends.SelectMany(x => x.OptimizerTypes));
        var knownBackends = DeduplicateStable(backends.Select(x => x.BackendDescriptor), static x => x.BackendId);
        var requiredInfrastructure = DeduplicateStable(
            shape.RequiredFrontendInfrastructureModuleTypes.Concat(shape.RequiredIrInfrastructureModuleTypes));

        return new WistDialectExecutionConfiguration(
            shape.DialectName,
            shape.FrontendModuleTypes,
            shape.IrModuleTypes,
            allOptimizers,
            backends,
            knownBackends,
            requiredInfrastructure);
    }

    private static List<T> DeduplicateStable<T>(IEnumerable<T> values)
        where T : notnull
    {
        var snapshot = new List<T>();
        var seen = new HashSet<T>();

        foreach (var value in values)
        {
            if (seen.Add(value))
                snapshot.Add(value);
        }

        return snapshot;
    }

    private static List<T> DeduplicateStable<T, TKey>(IEnumerable<T> values, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        var snapshot = new List<T>();
        var seen = new HashSet<TKey>();

        foreach (var value in values)
        {
            if (seen.Add(keySelector(value)))
                snapshot.Add(value);
        }

        return snapshot;
    }
}