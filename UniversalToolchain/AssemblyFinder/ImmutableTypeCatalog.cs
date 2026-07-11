namespace AssemblyFinder;

/// <summary>
///     Immutable catalog built from an explicit assembly allowlist.
/// </summary>
public sealed class ImmutableTypeCatalog : ITypeCatalog
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<Type>> _typesByFullName;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<Type>> _typesByShortName;

    public ImmutableTypeCatalog(IEnumerable<Assembly> assemblies)
    {
        assemblies = assemblies.ArgNotNull();

        Assemblies = assemblies
            .Where(static assembly => !assembly.IsDynamic)
            .Distinct()
            .OrderBy(static assembly => assembly.FullName, StringComparer.Ordinal)
            .ToArray();

        Types = Assemblies
            .SelectMany(GetLoadableTypes)
            .Distinct()
            .OrderBy(static type => type.FullName ?? type.Name, StringComparer.Ordinal)
            .ThenBy(static type => type.Assembly.FullName, StringComparer.Ordinal)
            .ToArray();

        _typesByFullName = BuildIndex(Types.Where(static type => type.FullName != null), static type => type.FullName!);
        _typesByShortName = BuildIndex(Types, static type => type.Name);
    }

    public IReadOnlyList<Assembly> Assemblies { get; }

    public IReadOnlyList<Type> Types { get; }

    public bool TryResolveType(string name, [NotNullWhen(true)] out Type? type)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Type name must not be empty.");

        var normalizedName = name.Trim();

        if (TryResolveUnique(_typesByFullName, normalizedName, out type))
            return true;

        if (TryResolveAssemblyQualifiedName(normalizedName, out type))
            return true;

        return TryResolveUnique(_typesByShortName, normalizedName, out type);
    }

    public Type ResolveRequiredType(string name)
    {
        if (TryResolveType(name, out var type))
            return type;

        throw new TypeLoadException($"Type '{name}' was not found in the explicit type catalog.");
    }

    private bool TryResolveAssemblyQualifiedName(string name, [NotNullWhen(true)] out Type? type)
    {
        var matches = Types
            .Where(candidate => string.Equals(candidate.AssemblyQualifiedName, name, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return ResolveUnique(name, matches, out type);
    }

    private static bool TryResolveUnique(
        IReadOnlyDictionary<string, IReadOnlyList<Type>> index,
        string name,
        [NotNullWhen(true)] out Type? type)
    {
        if (!index.TryGetValue(name, out var matches))
        {
            type = null;
            return false;
        }

        return ResolveUnique(name, matches, out type);
    }

    private static bool ResolveUnique(
        string requestedName,
        IReadOnlyCollection<Type> matches,
        [NotNullWhen(true)] out Type? type)
    {
        switch (matches.Count)
        {
            case 0:
                type = null;
                return false;
            case 1:
                type = matches.Single();
                return true;
            default:
                var candidates = string.Join(", ", matches
                    .Select(static candidate => candidate.AssemblyQualifiedName ?? candidate.FullName ?? candidate.Name)
                    .OrderBy(static candidate => candidate, StringComparer.Ordinal));
                throw new AmbiguousMatchException(
                    $"Type name '{requestedName}' is ambiguous in the explicit type catalog. Candidates: {candidates}.");
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<Type>> BuildIndex(
        IEnumerable<Type> types,
        Func<Type, string> keySelector) =>
        types
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<Type>)group
                    .OrderBy(static type => type.AssemblyQualifiedName, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
        catch (Exception exception) when (exception is NotSupportedException or FileLoadException)
        {
            return [];
        }
    }
}
