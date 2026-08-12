using System.Collections;
using System.Reflection;

namespace UniversalToolchain.Wist;

internal static class WistArgumentReader
{
    public static IReadOnlyDictionary<string, object?> FromObject(object arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments is IReadOnlyDictionary<string, object?> readOnlyDictionary)
            return readOnlyDictionary;

        if (arguments is IReadOnlyDictionary<string, double> doubleDictionary)
            return doubleDictionary.ToDictionary(static x => x.Key, static x => (object?)x.Value, StringComparer.Ordinal);

        if (arguments is IDictionary dictionary)
            return FromDictionary(dictionary);

        var properties = arguments.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static x => x.GetIndexParameters().Length == 0)
            .OrderBy(static x => x.MetadataToken)
            .ToArray();

        if (properties.Length == 0)
            throw new WistUserInputException("Argument object must expose at least one public readable property.", nameof(arguments));

        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            if (!property.CanRead)
                continue;

            AddArgument(result, property.Name, property.GetValue(arguments));
        }

        return result;
    }

    public static IReadOnlyDictionary<string, Type> TypesFromNamesAndTypes(params (string Name, Type Type)[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var result = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var argument in arguments)
            AddArgumentType(result, argument.Name, argument.Type);

        return result;
    }

    private static IReadOnlyDictionary<string, object?> FromDictionary(IDictionary dictionary)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not string name)
                throw new WistUserInputException("Dictionary argument keys must be strings.", nameof(dictionary));

            AddArgument(result, name, entry.Value);
        }

        return result;
    }

    private static void AddArgumentType(IDictionary<string, Type> target, string name, Type type)
    {
        ValidateName(name);

        if (target.ContainsKey(name))
            throw new WistUserInputException($"Duplicate Wist argument name '{name}'.", nameof(target));

        target[name] = type ?? throw new WistUserInputException("Wist argument types must not be null.", nameof(type));
    }

    private static void AddArgument(IDictionary<string, object?> target, string name, object? value)
    {
        ValidateName(name);

        if (target.ContainsKey(name))
            throw new WistUserInputException($"Duplicate Wist argument name '{name}'.", nameof(target));

        target[name] = value;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new WistUserInputException("Wist argument names must not be empty.", nameof(name));
    }
}
