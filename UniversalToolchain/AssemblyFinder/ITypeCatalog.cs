namespace AssemblyFinder;

/// <summary>
///     Provides a deterministic, immutable catalog of host-approved CLR types.
/// </summary>
public interface ITypeCatalog
{
    IReadOnlyList<Assembly> Assemblies { get; }

    IReadOnlyList<Type> Types { get; }

    bool TryResolveType(string name, [NotNullWhen(true)] out Type? type);

    Type ResolveRequiredType(string name);
}
