namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Defines whether a runtime host owns the lifetime of its service provider.
/// </summary>
public enum ServiceProviderOwnership
{
    Borrowed,
    Owned
}
