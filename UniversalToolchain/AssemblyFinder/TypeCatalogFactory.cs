namespace AssemblyFinder;

public static class TypeCatalogFactory
{
    /// <summary>
    ///     Creates an immutable catalog from exactly the assemblies selected by the host.
    /// </summary>
    public static ImmutableTypeCatalog Create(IEnumerable<Assembly> allowedAssemblies) =>
        new(allowedAssemblies.ArgNotNull());

    /// <summary>
    ///     Compatibility factory. No CLR assemblies are exposed unless they are supplied explicitly.
    /// </summary>
    [Obsolete("Use Create(allowedAssemblies). The catalog no longer exposes implicit CLR assemblies.")]
    public static ImmutableTypeCatalog CreateDefault(IEnumerable<Assembly>? allowedAssemblies = null) =>
        Create(allowedAssemblies ?? Array.Empty<Assembly>());
}
