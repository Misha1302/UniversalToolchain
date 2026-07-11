using ExceptionsManager;
namespace UniversalToolchain.Wist;

/// <summary>
///     In-process preflight limits enforced by the Wist facade.
/// </summary>
public sealed class WistResourceLimits
{
    public const int DefaultMaxSourceLength = 65_536;
    public const int DefaultMaxParameterCount = 64;

    /// <summary>
    ///     Gets or sets the maximum UTF-16 source length accepted by one facade operation.
    /// </summary>
    public int MaxSourceLength { get; set; } = DefaultMaxSourceLength;

    /// <summary>
    ///     Gets or sets the maximum number of declared external parameters.
    /// </summary>
    public int MaxParameterCount { get; set; } = DefaultMaxParameterCount;

    internal WistResourceLimits SnapshotValidated()
    {
        if (MaxSourceLength <= 0)
            Thrower.ArgumentOutOfRange<int>(nameof(MaxSourceLength), "Maximum source length must be positive.");

        if (MaxParameterCount < 0)
            Thrower.ArgumentOutOfRange<int>(nameof(MaxParameterCount), "Maximum parameter count must not be negative.");

        return new WistResourceLimits
        {
            MaxSourceLength = MaxSourceLength,
            MaxParameterCount = MaxParameterCount
        };
    }
}
