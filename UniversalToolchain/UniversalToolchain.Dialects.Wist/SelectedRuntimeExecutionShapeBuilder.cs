using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Builds the execution-facing shape for a resolved runtime selection.
/// </summary>
public sealed class SelectedRuntimeExecutionShapeBuilder
{
    private readonly SelectedRuntimeModuleClassifier _moduleClassifier;
    private readonly IWistRequiredInfrastructureModulesProvider _requiredInfrastructureModulesProvider;

    public SelectedRuntimeExecutionShapeBuilder(
        SelectedRuntimeModuleClassifier moduleClassifier,
        IWistRequiredInfrastructureModulesProvider requiredInfrastructureModulesProvider)
    {
        _moduleClassifier = moduleClassifier.ArgNotNull();
        _requiredInfrastructureModulesProvider = requiredInfrastructureModulesProvider.ArgNotNull();
    }

    public SelectedRuntimeExecutionShape Build(DialectBuildPlan buildPlan, SelectedRuntimePlan selectedRuntimePlan)
    {
        buildPlan = buildPlan.ArgNotNull();
        selectedRuntimePlan = selectedRuntimePlan.ArgNotNull();

        if (!selectedRuntimePlan.IsResolved)
            Thrower.Argument(nameof(selectedRuntimePlan), "Selected runtime plan must be resolved before execution wiring is built.");

        var selectedModules = _moduleClassifier.Classify(selectedRuntimePlan.OrderedModules);
        var requiredFrontendModuleTypes = _requiredInfrastructureModulesProvider.GetFrontendModuleTypes();
        var requiredIrModuleTypes = _requiredInfrastructureModulesProvider.GetIrModuleTypes();

        return new SelectedRuntimeExecutionShape(
            buildPlan.Name,
            requiredFrontendModuleTypes,
            selectedModules.FrontendModuleTypes,
            requiredIrModuleTypes,
            selectedModules.IrModuleTypes,
            selectedRuntimePlan.EnabledOptimizers,
            selectedRuntimePlan.EnabledBackends);
    }
}