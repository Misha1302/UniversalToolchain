namespace UniversalToolchain.PlanFuzz.Cli;

internal sealed class PlanFuzzCommandLine
{
    private readonly IReadOnlyDictionary<string, string?> _options;

    private PlanFuzzCommandLine(IReadOnlyList<string> positionals, IReadOnlyDictionary<string, string?> options)
    {
        Positionals = positionals;
        _options = options;
    }

    public IReadOnlyList<string> Positionals { get; }

    public static PlanFuzzCommandLine Parse(IEnumerable<string> arguments)
    {
        arguments = arguments.ArgNotNull();
        var values = arguments.ToArray();
        var positionals = new List<string>();
        var options = new Dictionary<string, string?>(StringComparer.Ordinal);
        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            if (!value.StartsWith("--", StringComparison.Ordinal))
            {
                positionals.Add(value);
                continue;
            }

            var separator = value.IndexOf('=', StringComparison.Ordinal);
            string name;
            string? optionValue;
            if (separator >= 0)
            {
                name = value[..separator];
                optionValue = value[(separator + 1)..];
            }
            else
            {
                name = value;
                optionValue = index + 1 < values.Length && !values[index + 1].StartsWith("--", StringComparison.Ordinal)
                    ? values[++index]
                    : null;
            }

            if (!options.TryAdd(name, optionValue))
                return Thrower.Argument<PlanFuzzCommandLine>(nameof(arguments), $"Option '{name}' was supplied more than once.");
        }
        return new PlanFuzzCommandLine(
            new ReadOnlyCollection<string>(positionals),
            new ReadOnlyDictionary<string, string?>(options));
    }

    public bool HasOption(string name) => _options.ContainsKey(name);

    public string GetRequired(string name)
    {
        if (!_options.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
            return Thrower.Argument<string>(name, $"Required option '{name}' is missing.");
        return value;
    }

    public string? GetOptional(string name) =>
        _options.TryGetValue(name, out var value) ? value : null;

    public int GetInt32(string name, int defaultValue, int minimum = int.MinValue)
    {
        var value = GetOptional(name);
        if (value == null)
            return defaultValue;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < minimum)
            return Thrower.Argument<int>(name, $"Option '{name}' must be an integer greater than or equal to {minimum.ToString(CultureInfo.InvariantCulture)}.");
        return parsed;
    }

    public long GetInt64(string name, long defaultValue, long minimum = long.MinValue)
    {
        var value = GetOptional(name);
        if (value == null)
            return defaultValue;
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < minimum)
            return Thrower.Argument<long>(name, $"Option '{name}' must be an integer greater than or equal to {minimum.ToString(CultureInfo.InvariantCulture)}.");
        return parsed;
    }

    public ulong GetUInt64(string name, ulong defaultValue)
    {
        var value = GetOptional(name);
        if (value == null)
            return defaultValue;
        if (!ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return Thrower.Argument<ulong>(name, $"Option '{name}' must be an unsigned 64-bit integer.");
        return parsed;
    }
}
