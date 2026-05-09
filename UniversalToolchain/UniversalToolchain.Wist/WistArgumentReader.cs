using ExceptionsManager;
using System.Collections;
using System.Reflection;

namespace UniversalToolchain.Wist;

internal static class WistArgumentReader
{
    public static IReadOnlyDictionary<string, object?> FromObject(object arguments)
    {
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
            Thrower.Argument(nameof(arguments), "Argument object must expose at least one public readable property.");

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
        var result = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var argument in arguments)
        {
            ValidateName(argument.Name);
            result[argument.Name] = argument.Type;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, object?> FromDictionary(IDictionary dictionary)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not string name)
            {
                Thrower.Argument(nameof(dictionary), "Dictionary argument keys must be strings.");
                continue;
            }

            AddArgument(result, name, entry.Value);
        }

        return result;
    }

    private static void AddArgument(IDictionary<string, object?> target, string name, object? value)
    {
        ValidateName(name);

        if (target.ContainsKey(name))
            Thrower.Argument(nameof(target), $"Duplicate Wist argument name '{name}'.");

        target[name] = value;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Wist argument names must not be empty.");
    }
}
