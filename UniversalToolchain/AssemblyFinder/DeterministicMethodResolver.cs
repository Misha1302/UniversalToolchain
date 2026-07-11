namespace AssemblyFinder;

/// <summary>
///     Deterministic method resolver over an immutable explicit type catalog.
/// </summary>
public sealed class DeterministicMethodResolver : IMethodResolver
{
    private const BindingFlags SupportedBindingFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

    private readonly ITypeCatalog _typeCatalog;
    private readonly ConcurrentDictionary<string, MethodInfo?> _cache = new(StringComparer.Ordinal);

    public DeterministicMethodResolver(ITypeCatalog typeCatalog)
    {
        _typeCatalog = typeCatalog.ArgNotNull();
    }

    public MethodInfo? GetMethod(string fullName) =>
        _cache.GetOrAdd($"any:{fullName}", _ => Resolve(fullName, null, null));

    public MethodInfo? GetMethod(string fullName, int parameterCount)
    {
        if (parameterCount < 0)
            Thrower.ArgumentOutOfRange<int>(nameof(parameterCount));

        return _cache.GetOrAdd($"count:{parameterCount}:{fullName}", _ => Resolve(fullName, parameterCount, null));
    }

    public MethodInfo? GetMethod(string fullName, IReadOnlyList<Type> parameterTypes)
    {
        parameterTypes = parameterTypes.ArgNotNull();
        var key = $"types:{string.Join("|", parameterTypes.Select(static type => type.AssemblyQualifiedName))}:{fullName}";
        return _cache.GetOrAdd(key, _ => Resolve(fullName, parameterTypes.Count, parameterTypes));
    }

    public bool CanResolveDeclaringType(string fullName)
    {
        if (!TrySplitName(fullName, out var typeName, out _))
            return false;

        return _typeCatalog.TryResolveType(typeName, out _);
    }

    public bool ContainsAnyMethod(string fullName)
    {
        if (!TrySplitName(fullName, out var typeName, out var methodName))
            return false;

        if (!_typeCatalog.TryResolveType(typeName, out var type))
            return false;

        return GetCandidates(type, methodName).Count != 0;
    }

    private MethodInfo? Resolve(string fullName, int? parameterCount, IReadOnlyList<Type>? parameterTypes)
    {
        if (!TrySplitName(fullName, out var typeName, out var methodName))
            return null;

        if (!_typeCatalog.TryResolveType(typeName, out var type))
            return null;

        var candidates = GetCandidates(type, methodName);
        if (parameterCount is { } count)
            candidates = candidates.Where(method => method.GetParameters().Length == count).ToArray();

        if (parameterTypes != null)
        {
            candidates = candidates
                .Select(method => TryCloseGenericMethod(method, parameterTypes))
                .Where(static method => method != null)
                .Cast<MethodInfo>()
                .ToArray();

            var exactMatches = candidates
                .Where(method => ParametersExactlyMatch(method, parameterTypes))
                .ToArray();

            if (exactMatches.Length != 0)
                return ResolveUnique(fullName, exactMatches);

            candidates = candidates
                .Where(method => ParametersAreCompatible(method, parameterTypes))
                .ToArray();
        }

        return ResolveUnique(fullName, candidates);
    }

    private static IReadOnlyList<MethodInfo> GetCandidates(Type type, string methodName) =>
        type.GetMethods(SupportedBindingFlags)
            .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
            .OrderBy(GetStableSignature, StringComparer.Ordinal)
            .ToArray();

    private static MethodInfo? ResolveUnique(string requestedName, IReadOnlyCollection<MethodInfo> candidates)
    {
        switch (candidates.Count)
        {
            case 0:
                return null;
            case 1:
                return candidates.Single();
            default:
                var signatures = string.Join(", ", candidates
                    .Select(GetStableSignature)
                    .OrderBy(static signature => signature, StringComparer.Ordinal));
                throw new AmbiguousMatchException(
                    $"Method reference '{requestedName}' is ambiguous. Candidates: {signatures}.");
        }
    }


    private static MethodInfo? TryCloseGenericMethod(MethodInfo method, IReadOnlyList<Type> parameterTypes)
    {
        if (!method.ContainsGenericParameters)
            return method;

        if (!method.IsGenericMethodDefinition)
            return null;

        var parameters = method.GetParameters();
        if (parameters.Length != parameterTypes.Count)
            return null;

        var inferredTypes = new Dictionary<Type, Type>();
        for (var index = 0; index < parameters.Length; index++)
        {
            if (!TryInferGenericArguments(parameters[index].ParameterType, parameterTypes[index], inferredTypes))
                return null;
        }

        var genericArguments = method.GetGenericArguments();
        if (genericArguments.Any(argument => !inferredTypes.ContainsKey(argument)))
            return null;

        try
        {
            return method.MakeGenericMethod(genericArguments.Select(argument => inferredTypes[argument]).ToArray());
        }
        catch (ArgumentException)
        {
            // The inferred types do not satisfy the method's generic constraints.
            return null;
        }
    }

    private static bool TryInferGenericArguments(
        Type parameterPattern,
        Type argumentType,
        IDictionary<Type, Type> inferredTypes)
    {
        if (parameterPattern.IsGenericParameter)
        {
            if (inferredTypes.TryGetValue(parameterPattern, out var existingType))
                return existingType == argumentType;

            inferredTypes.Add(parameterPattern, argumentType);
            return true;
        }

        if (parameterPattern.IsByRef)
        {
            return argumentType.IsByRef
                   && TryInferGenericArguments(
                       parameterPattern.GetElementType().NotNull(),
                       argumentType.GetElementType().NotNull(),
                       inferredTypes);
        }

        if (parameterPattern.IsArray)
        {
            return argumentType.IsArray
                   && parameterPattern.GetArrayRank() == argumentType.GetArrayRank()
                   && TryInferGenericArguments(
                       parameterPattern.GetElementType().NotNull(),
                       argumentType.GetElementType().NotNull(),
                       inferredTypes);
        }

        if (!parameterPattern.IsGenericType)
            return true;

        var patternDefinition = parameterPattern.GetGenericTypeDefinition();
        var matchingArgumentType = FindConstructedGenericType(argumentType, patternDefinition);
        if (matchingArgumentType == null)
            return false;

        var patternArguments = parameterPattern.GetGenericArguments();
        var actualArguments = matchingArgumentType.GetGenericArguments();
        for (var index = 0; index < patternArguments.Length; index++)
        {
            if (!TryInferGenericArguments(patternArguments[index], actualArguments[index], inferredTypes))
                return false;
        }

        return true;
    }

    private static Type? FindConstructedGenericType(Type type, Type genericTypeDefinition)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition)
            return type;

        var matchingInterface = type.GetInterfaces()
            .Where(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == genericTypeDefinition)
            .OrderBy(static candidate => candidate.AssemblyQualifiedName, StringComparer.Ordinal)
            .FirstOrDefault();
        if (matchingInterface != null)
            return matchingInterface;

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericTypeDefinition)
                return current;
        }

        return null;
    }

    private static bool ParametersExactlyMatch(MethodInfo method, IReadOnlyList<Type> parameterTypes)
    {
        var parameters = method.GetParameters();
        return parameters.Length == parameterTypes.Count
               && parameters.Select(static parameter => parameter.ParameterType).SequenceEqual(parameterTypes);
    }

    private static bool ParametersAreCompatible(MethodInfo method, IReadOnlyList<Type> parameterTypes)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != parameterTypes.Count)
            return false;

        for (var index = 0; index < parameters.Length; index++)
        {
            if (!IsTypeCompatible(parameterTypes[index], parameters[index].ParameterType))
                return false;
        }

        return true;
    }

    private static bool IsTypeCompatible(Type source, Type target)
    {
        if (source == target || target.IsAssignableFrom(source))
            return true;

        if (Nullable.GetUnderlyingType(target) is { } underlyingType)
            return IsTypeCompatible(source, underlyingType);

        if (!target.IsGenericParameter)
            return false;

        return target.GetGenericParameterConstraints().All(constraint => IsTypeCompatible(source, constraint));
    }

    private static bool TrySplitName(string fullName, out string typeName, out string methodName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            typeName = string.Empty;
            methodName = string.Empty;
            return false;
        }

        var dotIndex = fullName.LastIndexOf('.');
        if (dotIndex <= 0 || dotIndex == fullName.Length - 1)
        {
            typeName = string.Empty;
            methodName = string.Empty;
            return false;
        }

        typeName = fullName[..dotIndex];
        methodName = fullName[(dotIndex + 1)..];
        return true;
    }

    private static string GetStableSignature(MethodInfo method)
    {
        var declaringType = method.DeclaringType?.FullName ?? "<unknown>";
        var parameters = string.Join(",", method.GetParameters().Select(static parameter => parameter.ParameterType.FullName));
        return $"{declaringType}.{method.Name}({parameters}):{method.ReturnType.FullName}";
    }
}
