namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Resolves compile-time dialect group descriptors by alias.
/// </summary>
public interface IDialectGroupCatalog
{
    /// <summary>
    ///     Attempts to resolve a dialect group by alias.
    /// </summary>
    bool TryResolveGroup(string alias, out DialectGroupDescriptor? descriptor);

    /// <summary>
    ///     Gets all known dialect groups in deterministic order.
    /// </summary>
    IReadOnlyList<DialectGroupDescriptor> GetGroups();
}