namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Provides declarative dialect groups for compile-time dialect normalization.
/// </summary>
public interface IDialectGroupProvider
{
    /// <summary>
    ///     Gets dialect groups exposed by this provider.
    /// </summary>
    IReadOnlyList<DialectGroupDescriptor> GetGroups();
}