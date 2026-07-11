namespace AssemblyFinder;

/// <summary>
///     Resolves methods only from an explicitly supplied type catalog.
/// </summary>
public interface IMethodResolver
{
    MethodInfo? GetMethod(string fullName);

    MethodInfo? GetMethod(string fullName, int parameterCount);

    MethodInfo? GetMethod(string fullName, IReadOnlyList<Type> parameterTypes);

    bool CanResolveDeclaringType(string fullName);

    bool ContainsAnyMethod(string fullName);
}
