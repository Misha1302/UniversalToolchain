namespace UniversalToolchain.Wist;

/// <summary>
/// Stable public classification for failures that Wist deliberately exposes as structured results.
/// Infrastructure and internal failures are classified for diagnostics policy but are fail-fast by default.
/// </summary>
public enum WistFailureKind
{
    UserInput,
    Policy,
    Unsupported,
    Infrastructure,
    Internal
}
