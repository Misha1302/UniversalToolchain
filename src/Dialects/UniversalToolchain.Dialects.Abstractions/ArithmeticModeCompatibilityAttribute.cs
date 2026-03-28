namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Declares arithmetic mode compatibility for a concrete auto-registered type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ArithmeticModeCompatibilityAttribute : Attribute
{
    private readonly HashSet<ArithmeticMode> _supportedModes;

    public ArithmeticModeCompatibilityAttribute(params ArithmeticMode[] supportedModes)
    {
        if (supportedModes == null || supportedModes.Length == 0)
            throw new ArgumentException("At least one arithmetic mode must be specified.", nameof(supportedModes));

        _supportedModes = supportedModes.ToHashSet();
    }

    public bool Supports(ArithmeticMode mode) => _supportedModes.Contains(mode);
}