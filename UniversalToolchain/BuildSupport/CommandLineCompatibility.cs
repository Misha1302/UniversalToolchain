using System.Reflection;

namespace CommandLine;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class VerbAttribute : Attribute
{
    public VerbAttribute(string name)
    {
        Name = name;
    }

    public VerbAttribute(string name, bool isDefault)
    {
        Name = name;
        IsDefault = isDefault;
    }

    public string Name { get; }

    public bool IsDefault { get; }

    public string? HelpText { get; set; }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
internal sealed class OptionAttribute : Attribute
{
    public OptionAttribute(string longName)
    {
        LongName = longName;
    }

    public OptionAttribute(char shortName, string longName)
    {
        ShortName = shortName;
        LongName = longName;
    }

    public char? ShortName { get; }

    public string LongName { get; }

    public bool Required { get; set; }

    public object? Default { get; set; }

    public string? HelpText { get; set; }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
internal sealed class ValueAttribute : Attribute
{
    public ValueAttribute(int index)
    {
        Index = index;
    }

    public int Index { get; }

    public string? MetaName { get; set; }

    public bool Required { get; set; }

    public string? HelpText { get; set; }
}

internal sealed class Parser
{
    public static Parser Default { get; } = new();

    public ParserResult<object> ParseArguments<T1, T2, T3, T4, T5>(IEnumerable<string> args)
    {
        var tokens = args.ToArray();
        var optionType = ResolveVerbType(tokens, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
        return ParserResult<object>.Parsed(Parse(optionType, tokens));
    }

    private static Type ResolveVerbType(string[] args, params Type[] optionTypes)
    {
        var first = args.FirstOrDefault(static arg => !arg.StartsWith("-", StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(first))
        {
            var match = optionTypes.FirstOrDefault(type => string.Equals(GetVerb(type).Name, first, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
        }

        return optionTypes.FirstOrDefault(type => GetVerb(type).IsDefault) ?? optionTypes[0];
    }

    private static VerbAttribute GetVerb(Type optionType) =>
        optionType.GetCustomAttribute<VerbAttribute>() ?? new VerbAttribute(optionType.Name);

    private static object Parse(Type optionType, string[] args)
    {
        var result = Activator.CreateInstance(optionType) ?? throw new InvalidOperationException($"Cannot create options of type {optionType.FullName}.");
        var verb = GetVerb(optionType);
        var index = args.Length > 0 && string.Equals(args[0], verb.Name, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var positionals = new List<string>();

        while (index < args.Length)
        {
            var token = args[index];
            if (!token.StartsWith("-", StringComparison.Ordinal))
            {
                positionals.Add(token);
                index++;
                continue;
            }

            var property = FindOptionProperty(optionType, token);
            if (property == null)
            {
                index++;
                continue;
            }

            if (property.PropertyType == typeof(bool))
            {
                property.SetValue(result, true);
                index++;
                continue;
            }

            if (index + 1 >= args.Length)
                throw new ArgumentException($"Missing value for option '{token}'.");

            property.SetValue(result, Convert.ChangeType(args[index + 1], Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType));
            index += 2;
        }

        ApplyDefaults(optionType, result);
        ApplyPositionals(optionType, result, positionals);
        ValidateRequired(optionType, result);
        return result;
    }

    private static PropertyInfo? FindOptionProperty(Type optionType, string token)
    {
        foreach (var property in GetAllProperties(optionType))
        {
            var option = property.GetCustomAttribute<OptionAttribute>();
            if (option == null)
                continue;

            if (token == $"--{option.LongName}" || option.ShortName.HasValue && token == $"-{option.ShortName.Value}")
                return property;
        }

        return null;
    }

    private static void ApplyDefaults(Type optionType, object result)
    {
        foreach (var property in GetAllProperties(optionType))
        {
            var option = property.GetCustomAttribute<OptionAttribute>();
            if (option?.Default != null && property.GetValue(result) == null)
                property.SetValue(result, option.Default);
        }
    }

    private static void ApplyPositionals(Type optionType, object result, IReadOnlyList<string> positionals)
    {
        foreach (var property in GetAllProperties(optionType))
        {
            var value = property.GetCustomAttribute<ValueAttribute>();
            if (value == null || value.Index >= positionals.Count)
                continue;

            property.SetValue(result, Convert.ChangeType(positionals[value.Index], Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType));
        }
    }

    private static void ValidateRequired(Type optionType, object result)
    {
        foreach (var property in GetAllProperties(optionType))
        {
            var option = property.GetCustomAttribute<OptionAttribute>();
            var value = property.GetValue(result);
            if (option?.Required == true && IsMissing(value))
                throw new ArgumentException($"Missing required option '--{option.LongName}'.");

            var positional = property.GetCustomAttribute<ValueAttribute>();
            if (positional?.Required == true && IsMissing(value))
                throw new ArgumentException($"Missing required value '{positional.MetaName ?? property.Name}'.");
        }
    }

    private static bool IsMissing(object? value) => value is null or "";

    private static IEnumerable<PropertyInfo> GetAllProperties(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            foreach (var property in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                yield return property;
        }
    }
}

internal sealed class ParserResult<T>
{
    private readonly T? _value;
    private readonly Exception? _error;

    private ParserResult(T? value, Exception? error)
    {
        _value = value;
        _error = error;
    }

    public static ParserResult<T> Parsed(T value) => new(value, null);

    public static ParserResult<T> Failed(Exception error) => new(default, error);

    public TResult MapResult<T1, T2, T3, T4, T5, TResult>(
        Func<T1, TResult> parsedFunc1,
        Func<T2, TResult> parsedFunc2,
        Func<T3, TResult> parsedFunc3,
        Func<T4, TResult> parsedFunc4,
        Func<T5, TResult> parsedFunc5,
        Func<IEnumerable<Error>, TResult> notParsedFunc)
    {
        if (_error != null)
            return notParsedFunc([new Error(_error.Message)]);

        return _value switch
        {
            T1 value => parsedFunc1(value),
            T2 value => parsedFunc2(value),
            T3 value => parsedFunc3(value),
            T4 value => parsedFunc4(value),
            T5 value => parsedFunc5(value),
            _ => notParsedFunc([new Error("Unsupported command.")])
        };
    }
}

internal sealed record Error(string Message);
