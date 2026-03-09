namespace DependencyInjection;

/// <summary>
///     Constants that standardize service lifetimes in the Wist project.
/// </summary>
public static class ServiceLifetime
{
    /// <summary>
    ///     Stateless services (recommended for most modules).
    /// </summary>
    public const Microsoft.Extensions.DependencyInjection.ServiceLifetime Static =
        Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton;

    /// <summary>
    ///     Services with execution state (recommended for parsers and executors).
    /// </summary>
    public const Microsoft.Extensions.DependencyInjection.ServiceLifetime Execution =
        Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient;

    /// <summary>
    ///     Services that should be created once per scope
    ///     (currently not used actively, but kept for future extension).
    /// </summary>
    public const Microsoft.Extensions.DependencyInjection.ServiceLifetime Scoped =
        Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped;
}