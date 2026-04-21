namespace Wistc;

internal sealed class WistCliCustomizedDialectBuilder
{
    public string BuildFromPreset(WistShippedDialectPreset basePreset, WistCliCustomizationRequest request)
    {
        basePreset = basePreset.ArgNotNull();
        request = request.ArgNotNull();

        var presetDialectText = ReadPresetDialectText(basePreset);
        var parsed = ParseDirectives(presetDialectText);
        var customizedModules = BuildCustomizedModuleList(parsed.Modules, request);

        return ComposeCustomizedDialect(customizedModules, parsed.Backends, parsed.Enables, parsed.Security, parsed.Capabilities);
    }

    private static string ReadPresetDialectText(WistShippedDialectPreset basePreset)
    {
        var filePath = new WistShippedDialectFileResolver().Resolve(basePreset);
        return File.ReadAllText(filePath);
    }

    private static ParsedDialectDirectives ParseDirectives(string dialectText)
    {
        var modules = new List<string>();
        var backends = new List<string>();
        var enables = new List<string>();
        var security = new List<string>();
        var capabilities = new List<string>();

        foreach (var rawLine in dialectText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("use ", StringComparison.Ordinal))
            {
                modules.AddRange(ParseCsvDirectiveValues(line[4..]));
                continue;
            }

            if (line.StartsWith("backend ", StringComparison.Ordinal))
            {
                backends.AddRange(ParseCsvDirectiveValues(line[8..]));
                continue;
            }

            if (line.StartsWith("enable ", StringComparison.Ordinal))
            {
                enables.AddRange(ParseCsvDirectiveValues(line[7..]));
                continue;
            }

            if (line.StartsWith("security ", StringComparison.Ordinal))
            {
                security.AddRange(ParseCsvDirectiveValues(line[9..]));
                continue;
            }

            if (line.StartsWith("capability ", StringComparison.Ordinal))
                capabilities.AddRange(ParseCsvDirectiveValues(line[11..]));
        }

        if (modules.Count == 0)
            Thrower.InvalidOpEx("Shipped preset dialect must contain at least one use directive.");

        return new ParsedDialectDirectives(
            modules.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            backends.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            enables.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            security.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            capabilities.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static IReadOnlyList<string> BuildCustomizedModuleList(
        IReadOnlyList<string> baseModules,
        WistCliCustomizationRequest request)
    {
        var modules = baseModules.ToList();
        modules.AddRange(request.IncludeModules);

        if (request.ExcludeModules.Count > 0)
        {
            var excluded = new HashSet<string>(request.ExcludeModules, StringComparer.OrdinalIgnoreCase);
            modules = modules.Where(x => !excluded.Contains(x)).ToList();
        }

        return modules.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ComposeCustomizedDialect(
        IReadOnlyList<string> modules,
        IReadOnlyList<string> backends,
        IReadOnlyList<string> enables,
        IReadOnlyList<string> security,
        IReadOnlyList<string> capabilities)
    {
        var lines = new List<string>
        {
            "dialect CliCustomized",
            $"use {string.Join(",", modules)}"
        };

        if (backends.Count > 0)
            lines.Add($"backend {string.Join(",", backends)}");

        lines.AddRange(enables.Select(static x => $"enable {x}"));
        lines.AddRange(security.Select(static x => $"security {x}"));
        lines.AddRange(capabilities.Select(static x => $"capability {x}"));

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> ParseCsvDirectiveValues(string csv)
        => csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(static x => x.Trim())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .ToList();

    private sealed record ParsedDialectDirectives(
        IReadOnlyList<string> Modules,
        IReadOnlyList<string> Backends,
        IReadOnlyList<string> Enables,
        IReadOnlyList<string> Security,
        IReadOnlyList<string> Capabilities);
}
