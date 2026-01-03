using System.Collections.Concurrent;
using System.Reflection;

namespace AssemblyFinder;

public static class MethodsFinder
{
    private static readonly ConcurrentDictionary<string, MethodInfo?> _methodCache = new();
    private static readonly ConcurrentDictionary<string, MethodInfo?> _methodWithParamsCache = new();

    public static MethodInfo? GetMethod(string fullName) =>
        _methodCache.GetOrAdd(fullName, FindMethod);

    public static MethodInfo? GetMethod(string fullName, Type[] parameterTypes) =>
        _methodWithParamsCache.GetOrAdd($"{fullName}[{string.Join(",", parameterTypes.Select(t => t.FullName))}]",
            _ => FindMethod(fullName, parameterTypes));

    private static MethodInfo? FindMethod(string fullName)
    {
        var split = fullName.Split('.');
        if (split.Length < 2) return null;

        // Последний элемент - имя метода
        var methodName = split.Last();

        // Все остальные элементы - имя типа
        var typeNameParts = split.Take(split.Length - 1).ToList();

        // Сначала ищем по полному имени (Namespace.Class)
        var fullTypeName = string.Join(".", typeNameParts);
        var method = FindMethodInType(fullTypeName, methodName, null);
        if (method != null) return method;

        // Если не нашли, пробуем найти по короткому имени (Class)
        var shortTypeName = typeNameParts.Last();
        return FindMethodInType(shortTypeName, methodName, null);
    }

    private static MethodInfo? FindMethod(string fullName, Type[] parameterTypes)
    {
        var split = fullName.Split('.');
        if (split.Length < 2) return null;

        // Последний элемент - имя метода
        var methodName = split.Last();

        // Все остальные элементы - имя типа
        var typeNameParts = split.Take(split.Length - 1).ToList();

        // Сначала ищем по полному имени (Namespace.Class)
        var fullTypeName = string.Join(".", typeNameParts);
        var method = FindMethodInType(fullTypeName, methodName, parameterTypes);
        if (method != null) return method;

        // Если не нашли, пробуем найти по короткому имени (Class)
        var shortTypeName = typeNameParts.Last();
        return FindMethodInType(shortTypeName, methodName, parameterTypes);
    }

    private static MethodInfo? FindMethodInType(string typeName, string methodName, Type[]? parameterTypes)
    {
        // Ищем тип в сборках
        var type = FindTypeByName(typeName);
        if (type == null) return null;

        parameterTypes ??= [];

        // Ищем метод с определенными параметрами
        // Сначала точное совпадение
        var exactMethod = type.GetMethod(methodName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
            null, parameterTypes, null);

        if (exactMethod != null) return exactMethod;

        // Если не нашли точного совпадения, ищем по имени и количеству параметров
        // с проверкой совместимости типов
        var candidates = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Where(m => m.Name == methodName && m.GetParameters().Length == parameterTypes.Length)
            .ToList();

        if (candidates.Count == 1) return candidates[0];

        // Если несколько кандидатов, выбираем наиболее подходящий
        // (простая эвристика - первый метод, где все параметры совместимы)
        foreach (var candidate in candidates)
        {
            var parameters = candidate.GetParameters();
            var allCompatible = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                if (!IsTypeCompatible(parameterTypes[i], parameters[i].ParameterType))
                {
                    allCompatible = false;
                    break;
                }
            }

            if (allCompatible) return candidate;
        }

        return null;
    }

    private static Type? FindTypeByName(string typeName)
    {
        // Прямой поиск по полному имени
        var type = Type.GetType(typeName);
        if (type != null) return type;

        // Поиск во всех сборках
        var allTypes = TypesFinder.AllTypes;

        // Сначала ищем по полному имени (включая namespace)
        var foundTypes = allTypes
            .Where(t => t.FullName != null && t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (foundTypes.Count == 1) return foundTypes[0];

        // Если не нашли по полному имени, ищем по имени класса (без namespace)
        foundTypes = allTypes
            .Where(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (foundTypes.Count == 1) return foundTypes[0];

        // Если несколько типов с одним именем, предпочитаем не-generic типы
        var nonGenericTypes = foundTypes.Where(t => !t.IsGenericType).ToList();
        if (nonGenericTypes.Count == 1) return nonGenericTypes[0];

        // Если все еще несколько, возвращаем первый
        return foundTypes.FirstOrDefault();
    }

    private static bool IsTypeCompatible(Type source, Type target)
    {
        if (source == target) return true;
        if (target.IsAssignableFrom(source)) return true;

        // Проверка на числовые преобразования
        var numericTypes = new HashSet<Type>
        {
            typeof(int), typeof(long), typeof(float), typeof(double),
            typeof(decimal), typeof(short), typeof(byte), typeof(ushort),
            typeof(uint), typeof(ulong), typeof(sbyte), typeof(char)
        };

        if (numericTypes.Contains(source) && numericTypes.Contains(target))
            return true;

        // Проверка на nullable типы
        if (target.IsGenericType && target.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(target);
            return IsTypeCompatible(source, underlyingType!);
        }

        // Проверка на интерфейсы
        if (target.IsInterface && source.GetInterfaces().Contains(target))
            return true;

        // Для generics: попытка проверки через ограничения
        if (target.IsGenericParameter)
        {
            // Проверяем ограничения generic-параметра
            var constraints = target.GetGenericParameterConstraints();
            foreach (var constraint in constraints)
            {
                if (!IsTypeCompatible(source, constraint))
                    return false;
            }
            return true;
        }

        return false;
    }

    public static bool ContainsAnyMethod(string name)
    {
        var dotIndex = name.IndexOf('.');
        if (dotIndex == -1) return false;
        var leftPart = name[..dotIndex];
        var methodName = name[(dotIndex + 1)..];
        var type = FindTypeByName(leftPart);
        if (type == null) return false;

        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return method != null;
    }
}