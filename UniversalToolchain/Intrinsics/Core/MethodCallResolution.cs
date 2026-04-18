namespace BasicCore.Core;

/// <summary>
///     Describes the stack effect of a resolved .NET method call.
/// </summary>
public sealed class MethodCallResolution
{
    public required IReadOnlyList<Type> ConsumedTypes { get; init; }

    public required Type ReturnType { get; init; }
}