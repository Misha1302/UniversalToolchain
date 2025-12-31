namespace DependencyInjection;

/// <summary>
///     Константы для стандартизации времени жизни сервисов в проекте Wist
/// </summary>
public static class ServiceLifetime
{
    /// <summary>
    ///     Статические сервисы без состояния (рекомендуется для большинства модулей)
    /// </summary>
    public const Microsoft.Extensions.DependencyInjection.ServiceLifetime Static =
        Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton;

    /// <summary>
    ///     Сервисы с состоянием выполнения (рекомендуется для парсеров, исполнителей)
    /// </summary>
    public const Microsoft.Extensions.DependencyInjection.ServiceLifetime Execution =
        Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient;

    /// <summary>
    ///     Сервисы, которые должны быть созданы один раз для определенной области
    ///     (в настоящее время не используется активно, но оставлено для будущего расширения)
    /// </summary>
    public const Microsoft.Extensions.DependencyInjection.ServiceLifetime Scoped =
        Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped;
}