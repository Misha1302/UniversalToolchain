namespace AssemblyFinder;

public static class TypeCatalogFactory
{
    /// <summary>
    ///     Creates an immutable catalog from exactly the assemblies selected by the host.
    /// </summary>
    public static ImmutableTypeCatalog Create(IEnumerable<Assembly> allowedAssemblies) =>
        new(allowedAssemblies.ArgNotNull());
}
