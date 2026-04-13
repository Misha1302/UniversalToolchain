using System.Reflection;
using System.Text.Json;

var arguments = CliArguments.Parse(args);
var manifest = ManifestEmitter.Emit(arguments.AssemblyPath);
Directory.CreateDirectory(Path.GetDirectoryName(arguments.OutputPath)!);
File.WriteAllText(arguments.OutputPath, JsonSerializer.Serialize(manifest, JsonOptions.Instance));

internal sealed record CliArguments(string AssemblyPath, string OutputPath)
{
    public static CliArguments Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length - 1; i += 2)
            values[args[i]] = args[i + 1];

        if (!values.TryGetValue("--assembly", out var assemblyPath) || string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentException("Missing '--assembly <absolute path>' argument.");

        if (!Path.IsPathRooted(assemblyPath))
            throw new ArgumentException("--assembly must be an absolute path.");

        if (!values.TryGetValue("--output", out var outputPath) || string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Missing '--output <absolute path>' argument.");

        if (!Path.IsPathRooted(outputPath))
            throw new ArgumentException("--output must be an absolute path.");

        return new CliArguments(Path.GetFullPath(assemblyPath), Path.GetFullPath(outputPath));
    }
}

internal static class ManifestEmitter
{
    private const string RuntimeExportAttributeFullName = "UniversalToolchain.Dialects.Abstractions.DialectRuntimeExportAttribute";
    private const string RuntimeAliasAttributeFullName = "UniversalToolchain.Dialects.Abstractions.DialectRuntimeAliasAttribute";

    public static ManifestDocument Emit(string assemblyPath)
    {
        var resolver = new PathAssemblyResolver(BuildMetadataAssemblyPaths(assemblyPath));

        using var context = new MetadataLoadContext(resolver);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);

        var components = assembly
            .GetTypes()
            .SelectMany(BuildEntry)
            .OrderBy(static x => x.Kind, StringComparer.Ordinal)
            .ThenBy(static x => x.CanonicalAlias, StringComparer.Ordinal)
            .ThenBy(static x => x.ComponentId, StringComparer.Ordinal)
            .ToList();

        return new ManifestDocument(assembly.GetName().Name!, components);
    }

    private static IEnumerable<string> BuildMetadataAssemblyPaths(string assemblyPath)
    {
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        var runtimePaths = string.IsNullOrWhiteSpace(tpa)
            ? []
            : tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var localPaths = Directory.EnumerateFiles(Path.GetDirectoryName(assemblyPath)!, "*.dll", SearchOption.TopDirectoryOnly);
        return runtimePaths
            .Concat(localPaths)
            .Append(assemblyPath)
            .Distinct(StringComparer.Ordinal);
    }

    private static IEnumerable<ManifestComponentEntry> BuildEntry(Type type)
    {
        var export = type.CustomAttributes.FirstOrDefault(static x => x.AttributeType.FullName == RuntimeExportAttributeFullName);
        if (export == null)
            return [];

        var values = export.ConstructorArguments.Select(static x => x.Value?.ToString() ?? string.Empty).ToArray();
        if (values.Length < 2)
            return [];

        var kind = values[0];
        var canonicalAlias = values[1];

        var aliases = type.CustomAttributes
            .Where(static x => x.AttributeType.FullName == RuntimeAliasAttributeFullName)
            .Select(static x => x.ConstructorArguments[0].Value?.ToString())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();

        return [new ManifestComponentEntry(kind, canonicalAlias, aliases, RuntimeId(kind, canonicalAlias))];
    }

    private static string RuntimeId(string kind, string canonicalAlias)
    {
        var prefix = kind.Trim() switch
        {
            "FrontendModule" => "frontend",
            "Optimizer" => "optimizer",
            "Backend" => "backend",
            _ => throw CreateUnknownRuntimeKindException(kind)
        };

        return $"{prefix}.{canonicalAlias.Trim().ToLowerInvariant()}";
    }

    private static Exception CreateUnknownRuntimeKindException(string kind)
    {
        return new InvalidOperationException($"Unknown runtime component kind '{kind}'.");
    }
}

internal sealed record ManifestDocument(string AssemblySimpleName, IReadOnlyList<ManifestComponentEntry> Components);

internal sealed record ManifestComponentEntry(string Kind, string CanonicalAlias, IReadOnlyList<string> Aliases, string ComponentId);

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Instance = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
