using Microsoft.Extensions.DependencyInjection;

namespace BasicCore;

/// <summary>
/// Marks a class as a service that should be automatically registered
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AutoRegisterServiceAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Singleton;
    public Type? ServiceType { get; init; }
}