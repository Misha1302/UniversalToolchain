using System.Reflection;

namespace UniversalIntermediateRepresentation;

public static class GenericTypeResolver
{
    public static MethodInfo MakeGenericMethod(MethodInfo call, IReadOnlyList<Type> argTypes)
    {
        if (!call.ContainsGenericParameters) return call;

        var genericTypes = call.GetGenericArguments()
            .Select((x, i) => x.IsGenericMethodParameter ? argTypes[i] : x)
            .ToArray();

        return call.GetGenericMethodDefinition().MakeGenericMethod(genericTypes);
    }

    public static IReadOnlyList<Type> GetParameterTypes(MethodInfo method, List<Type> stack)
    {
        var types = (List<Type>)[];
        var parameters = method.GetParameters();
        var i = 0;
        foreach (var parameter in parameters)
        {
            var targetType = parameter.ParameterType is { ContainsGenericParameters: true, IsInterface: false }
                ? stack[i]
                : parameter.ParameterType;
            types.Add(targetType);
            i++;
        }
        return types;
    }
}