namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Registers dialect subsystem services into dependency injection.
/// </summary>
public interface IDialectServicesRegistrar
{
    /// <summary>
    ///     Registers dialect services with deterministic ordering.
    /// </summary>
    /// <param name="services">Service collection to extend.</param>
    void Register(IServiceCollection services);
}