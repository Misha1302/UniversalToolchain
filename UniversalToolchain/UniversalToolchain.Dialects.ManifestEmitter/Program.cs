using System.Reflection;
using System.Text.Json;

var arguments = CliArguments.Parse(args);
var manifest = ManifestEmitter.Emit(arguments.AssemblyPath, arguments.DialectFamily);
Directory.CreateDirectory(Path.GetDirectoryName(arguments.OutputPath)!);
File.WriteAllText(arguments.OutputPath, JsonSerializer.Serialize(manifest, JsonOptions.Instance));

internal sealed record CliArguments(string AssemblyPath, string DialectFamily, string OutputPath)
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

        if (!values.TryGetValue("--dialect-family", out var dialectFamily) || string.IsNullOrWhiteSpace(dialectFamily))
            throw new ArgumentException("Missing '--dialect-family <name>' argument.");

        if (!values.TryGetValue("--output", out var outputPath) || string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Missing '--output <absolute path>' argument.");

        if (!Path.IsPathRooted(outputPath))
            throw new ArgumentException("--output must be an absolute path.");

        return new CliArguments(Path.GetFullPath(assemblyPath), dialectFamily.Trim(), Path.GetFullPath(outputPath));
    }
}

internal static class ManifestEmitter
{
    private const string RuntimeExportAttributeFullName = "UniversalToolchain.Dialects.Abstractions.DialectRuntimeExportAttribute";
    private const string RuntimeAliasAttributeFullName = "UniversalToolchain.Dialects.Abstractions.DialectRuntimeAliasAttribute";

    public static ManifestDocument Emit(string assemblyPath, string dialectFamily)
    {
        var runtimePaths = Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll", SearchOption.TopDirectoryOnly);
        var localPaths = Directory.GetFiles(Path.GetDirectoryName(assemblyPath)!, "*.dll", SearchOption.TopDirectoryOnly);
        var resolver = new PathAssemblyResolver(runtimePaths.Concat(localPaths).Append(assemblyPath).Distinct(StringComparer.Ordinal));

        using var context = new MetadataLoadContext(resolver);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);

        var components = assembly
            .GetTypes()
            .SelectMany(type => BuildEntry(type, dialectFamily))
            .OrderBy(static x => x.Kind, StringComparer.Ordinal)
            .ThenBy(static x => x.CanonicalAlias, StringComparer.Ordinal)
            .ThenBy(static x => x.TypeFullName, StringComparer.Ordinal)
            .ToList();

        return new ManifestDocument(dialectFamily, assembly.GetName().Name!, components);
    }

    private static IEnumerable<ManifestComponentEntry> BuildEntry(Type type, string dialectFamily)
    {
        var export = type.CustomAttributes.FirstOrDefault(static x => x.AttributeType.FullName == RuntimeExportAttributeFullName);
        if (export == null)
            return [];

        var values = export.ConstructorArguments.Select(static x => x.Value?.ToString() ?? string.Empty).ToArray();
        if (values.Length < 3 || !string.Equals(values[0], dialectFamily, StringComparison.Ordinal))
            return [];

        var aliases = type.CustomAttributes
            .Where(static x => x.AttributeType.FullName == RuntimeAliasAttributeFullName)
            .Select(static x => x.ConstructorArguments[0].Value?.ToString())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();

        return [new ManifestComponentEntry(values[1], values[2], aliases, type.FullName ?? type.Name)];
    }
}

internal sealed record ManifestDocument(string DialectFamily, string AssemblySimpleName, IReadOnlyList<ManifestComponentEntry> Components);

internal sealed record ManifestComponentEntry(string Kind, string CanonicalAlias, IReadOnlyList<string> Aliases, string TypeFullName);

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Instance = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
