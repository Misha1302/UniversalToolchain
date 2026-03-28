namespace DependencyInjection;

/// <summary>
///     Options for Wist configuration.
/// </summary>
public class WistOptions
{
    /// <summary>
    ///     Selected arithmetic mode.
    /// </summary>
    public ArithmeticMode ArithmeticMode { get; set; } = ArithmeticMode.Universal;

    /// <summary>
    ///     Namespaces that should be excluded from automatic registration.
    /// </summary>
    public IReadOnlyList<string>? ExcludedNamespaces { get; set; }

    /// <summary>
    ///     Namespaces that should be included (all others will be excluded).
    /// </summary>
    public IReadOnlyList<string>? IncludedNamespaces { get; set; }

    /// <summary>
    ///     Concrete module types that should be removed.
    /// </summary>
    public IReadOnlyList<Type>? ModulesToRemove { get; set; }
}
