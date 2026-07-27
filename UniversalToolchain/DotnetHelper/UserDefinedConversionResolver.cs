using System.Reflection;

namespace DotnetHelper;

/// <summary>
/// Resolves an exact CLR user-defined conversion declared by either the source
/// or target type. Only standard one-argument implicit conversion methods
/// with an exact source parameter and target return type are accepted.
/// </summary>
public static class UserDefinedConversionResolver
{
    public static MethodInfo? Find(Type sourceType, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(targetType);

        if (sourceType == targetType || targetType.IsAssignableFrom(sourceType))
            return null;

        return EnumerateOwners(sourceType, targetType)
            .SelectMany(static owner => owner.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(static method => method.IsSpecialName && method.Name == "op_Implicit")
            .Where(method => method.ReturnType == targetType)
            .Where(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == sourceType;
            })
            .OrderBy(static method => method.DeclaringType?.FullName, StringComparer.Ordinal)
            .ThenBy(static method => method.MetadataToken)
            .FirstOrDefault();
    }

    public static bool CanConvert(Type sourceType, Type targetType) =>
        sourceType == targetType ||
        targetType.IsAssignableFrom(sourceType) ||
        Find(sourceType, targetType) != null;

    private static IEnumerable<Type> EnumerateOwners(Type sourceType, Type targetType)
    {
        yield return sourceType;
        if (targetType != sourceType)
            yield return targetType;
    }
}
