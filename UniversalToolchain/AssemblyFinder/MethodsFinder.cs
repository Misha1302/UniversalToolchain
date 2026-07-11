namespace AssemblyFinder;

/// <summary>
///     Fail-closed compatibility facade. Use an injected explicit resolver for real resolution.
/// </summary>
[Obsolete("Use an injected IMethodResolver backed by an explicit ITypeCatalog.")]
public static class MethodsFinder
{
    private static readonly IMethodResolver _compatibilityResolver =
        new DeterministicMethodResolver(TypeCatalogFactory.Create(Array.Empty<Assembly>()));

    public static MethodInfo? GetMethod(string fullName) => _compatibilityResolver.GetMethod(fullName);

    public static MethodInfo? GetMethod(string fullName, int parameterCount) =>
        _compatibilityResolver.GetMethod(fullName, parameterCount);

    public static MethodInfo? GetMethod(string fullName, Type[] parameterTypes) =>
        _compatibilityResolver.GetMethod(fullName, parameterTypes);

    public static bool CanResolveDeclaringType(string name) => _compatibilityResolver.CanResolveDeclaringType(name);

    public static bool ContainsAnyMethod(string name) => _compatibilityResolver.ContainsAnyMethod(name);
}
