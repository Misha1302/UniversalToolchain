namespace BasicCore.Attributes;

/// <summary>
///     Marks a class as a service that should be automatically registered
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AutoRegisterServiceAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Singleton;
    public Type? ServiceType { get; init; }
}