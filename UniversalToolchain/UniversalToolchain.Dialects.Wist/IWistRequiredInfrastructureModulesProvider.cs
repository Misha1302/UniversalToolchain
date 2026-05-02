namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Provides Wist infrastructure modules required outside dialect-selected runtime modules.
/// </summary>
public interface IWistRequiredInfrastructureModulesProvider
{
    IReadOnlyList<Type> GetFrontendModuleTypes();

    IReadOnlyList<Type> GetIRModuleTypes();
}