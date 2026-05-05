namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Provides the minimal Wist infrastructure needed to execute a selected runtime shape.
/// </summary>
public sealed class WistRequiredInfrastructureModulesProvider : IWistRequiredInfrastructureModulesProvider
{
    public IReadOnlyList<Type> GetFrontendModuleTypes() =>
        [typeof(ProgramStructureFrontendModule)];

    public IReadOnlyList<Type> GetIrModuleTypes() =>
        [];
}