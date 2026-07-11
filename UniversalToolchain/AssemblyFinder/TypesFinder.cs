namespace AssemblyFinder;

/// <summary>
///     Fail-closed compatibility facade. Use an injected explicit catalog for real resolution.
/// </summary>
[Obsolete("Use an injected ITypeCatalog. Process-wide discovery and late assembly registration are no longer supported.")]
public static class TypesFinder
{
    private static readonly ITypeCatalog _compatibilityCatalog = TypeCatalogFactory.Create(Array.Empty<Assembly>());

    public static IEnumerable<Assembly> Assemblies => _compatibilityCatalog.Assemblies;

    public static IEnumerable<Type> AllTypes => _compatibilityCatalog.Types;

    public static Type GetType(string name) => _compatibilityCatalog.ResolveRequiredType(name);

    [Obsolete("Build a new ImmutableTypeCatalog from an explicit assembly allowlist instead.", true)]
    public static void RegisterAssembly(Assembly assembly) =>
        throw new NotSupportedException("Late process-wide assembly registration was removed. Build an immutable explicit catalog per runtime host.");
}
