using System.Reflection;
using ExceptionsManager;

namespace DotnetHelper;

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

    public static IReadOnlyList<Type> GetParameterTypes(MethodInfo method, IReadOnlyList<Type> stack)
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

    public static MethodInfo ResolveOverloadedMethod(Type type, string methodName, IReadOnlyList<Type> argumentTypes)
    {
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == methodName)
            .ToList();

        if (methods.Count == 1) return methods[0];

        // Ищем лучшую перегрузку по совместимости типов
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != argumentTypes.Count) continue;

            var isMatch = true;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (!IsTypeCompatible(argumentTypes[i], parameters[i].ParameterType))
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch) return method;
        }

        // Если не нашли точного совпадения, возвращаем первую подходящую по количеству параметров
        return methods.FirstOrDefault(m => m.GetParameters().Length == argumentTypes.Count)
               ?? Thrower.InvalidOpEx<MethodInfo>($"No suitable overload found for {methodName}");
    }

    private static bool IsTypeCompatible(Type source, Type target)
    {
        if (source == target) return true;
        if (target.IsAssignableFrom(source)) return true;
        if (source.IsGenericType && target.IsGenericType &&
            source.GetGenericTypeDefinition() == target.GetGenericTypeDefinition())
        {
            return true;
        }

        // Проверка на числовые преобразования
        var numericTypes = new HashSet<Type>
        {
            typeof(int), typeof(long), typeof(float), typeof(double),
            typeof(decimal), typeof(short), typeof(byte)
        };

        if (numericTypes.Contains(source) && numericTypes.Contains(target))
            return true;

        return false;
    }
}