using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalToolchain.Dialects.Wist.Presets;

namespace Tests.Architecture;

[TestFixture]
public sealed partial class WistMigrationGateTests
{
    private static readonly HashSet<string> AllowedStatuses =
    [
        "Missing",
        "Partial",
        "Equivalent",
        "EquivalentWithKnownDifferences",
        "Deprecated",
        "Removed"
    ];

    [Test]
    public void ParityMatrix_CoversEveryProductionRuntimeExportAndShippedPreset()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "WIST_PARITY_MATRIX.json")));
        var matrix = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(matrix.GetProperty("genericPackPositioning").GetString(), Is.EqualTo("Wist subset alpha"));
            Assert.That(matrix.GetProperty("replacementClaimAllowed").GetBoolean(), Is.False);
        });

        var moduleAliases = ReadStringSet(matrix.GetProperty("modules"), "legacyAlias");
        var optimizerAliases = ReadStringSet(matrix.GetProperty("optimizers"), "legacyAlias");
        var backendIds = ReadStringSet(matrix.GetProperty("backends"), "typedBackendId");
        var exported = ScanProductionRuntimeExports(root);
        var violations = new List<string>();

        foreach (var export in exported)
        {
            var covered = export.Kind switch
            {
                "FrontendModule" => moduleAliases.Contains(export.Alias),
                "Optimizer" => optimizerAliases.Contains(export.Alias),
                "Backend" => backendIds.Contains(export.Alias),
                _ => true
            };
            if (!covered)
                violations.Add($"{export.Kind} '{export.Alias}' from {export.RelativePath} is missing from WIST_PARITY_MATRIX.json");
        }

        var presetIds = ReadStringSet(matrix.GetProperty("presets"), "id");
        foreach (var preset in WistShippedDialectPresets.All)
        {
            if (!presetIds.Contains(preset.Id))
                violations.Add($"Shipped preset '{preset.Id}' is missing from WIST_PARITY_MATRIX.json");
        }

        Assert.That(violations, Is.Empty);
    }

    [Test]
    public void ParityMatrix_EquivalentPresetRequiresExecutableEquivalenceTest()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "WIST_PARITY_MATRIX.json")));
        var violations = new List<string>();

        foreach (var sectionName in new[] { "modules", "optimizers", "backends", "presets" })
        {
            foreach (var entry in document.RootElement.GetProperty(sectionName).EnumerateArray())
            {
                var status = entry.GetProperty("status").GetString() ?? string.Empty;
                if (!AllowedStatuses.Contains(status))
                    violations.Add($"{sectionName} contains unsupported status '{status}'.");

                if (sectionName == "presets" && status is "Equivalent" or "EquivalentWithKnownDifferences")
                {
                    var tests = entry.GetProperty("tests").EnumerateArray().Select(static x => x.GetString()).Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    if (tests.Length == 0)
                        violations.Add($"Equivalent preset '{entry.GetProperty("id").GetString()}' has no executable equivalence test.");
                }
            }
        }

        var allPresetsEquivalent = document.RootElement.GetProperty("presets").EnumerateArray()
            .All(static entry => entry.GetProperty("status").GetString() is "Equivalent" or "EquivalentWithKnownDifferences");
        var replacementAllowed = document.RootElement.GetProperty("replacementClaimAllowed").GetBoolean();
        if (replacementAllowed != allPresetsEquivalent)
            violations.Add("replacementClaimAllowed must exactly match shipped-preset parity completion.");

        var markdown = File.ReadAllText(Path.Combine(root, "WIST_PARITY_MATRIX_RU.md"));
        if (!markdown.Contains("Wist subset alpha", StringComparison.Ordinal) ||
            !markdown.Contains("не полная замена", StringComparison.OrdinalIgnoreCase))
        {
            violations.Add("Public parity matrix must position the generic pack as Wist subset alpha, not a full replacement.");
        }

        Assert.That(violations, Is.Empty);
    }

    [Test]
    public void LegacyDeprecationRegistry_IsVersionedOwnedAndBlockedByParity()
    {
        var root = FindRepositoryRoot();
        using var registryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "LEGACY_DEPRECATION_REGISTRY.json")));
        using var matrixDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "WIST_PARITY_MATRIX.json")));
        var entries = registryDocument.RootElement.GetProperty("entries").EnumerateArray().ToArray();
        var violations = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var symbols = new HashSet<string>(StringComparer.Ordinal);
        var source = string.Join(
            Environment.NewLine,
            EnumerateProductionSources(root).Select(File.ReadAllText));

        foreach (var entry in entries)
        {
            var id = Required(entry, "id", violations);
            var symbol = Required(entry, "symbol", violations);
            Required(entry, "owner", violations);
            Required(entry, "replacement", violations);
            Required(entry, "firstDeprecatedVersion", violations);
            Required(entry, "warningAsErrorNotBefore", violations);
            Required(entry, "removalNotBefore", violations);
            Required(entry, "usageAssessment", violations);
            Required(entry, "migrationGuide", violations);
            Required(entry, "parityGate", violations);

            if (id.Length != 0 && !ids.Add(id))
                violations.Add($"Duplicate deprecation id '{id}'.");
            if (symbol.Length != 0 && !symbols.Add(symbol))
                violations.Add($"Duplicate deprecated symbol '{symbol}'.");
            if (id.Length != 0 && !source.Contains(id, StringComparison.Ordinal))
                violations.Add($"Deprecation id '{id}' is not present in a production [Obsolete] message.");
            if (entry.GetProperty("exitCriteria").GetArrayLength() == 0)
                violations.Add($"Deprecation entry '{id}' has no exit criteria.");
            if (entry.GetProperty("status").GetString() == "Removed")
                violations.Add($"Deprecation entry '{id}' is marked Removed before the parity gate is complete.");
        }

        var allPresetsEquivalent = matrixDocument.RootElement.GetProperty("presets").EnumerateArray()
            .All(static entry => entry.GetProperty("status").GetString() is "Equivalent" or "EquivalentWithKnownDifferences");
        if (!allPresetsEquivalent && entries.Any(static entry => entry.GetProperty("status").GetString() == "Removed"))
            violations.Add("Legacy API cannot be removed while any shipped preset remains Partial or Missing.");

        var migrationGuide = Path.Combine(root, "docs", "migration", "WIST_LEGACY_MIGRATION_RU.md");
        if (!File.Exists(migrationGuide))
            violations.Add("Wist legacy migration guide is missing.");

        Assert.That(violations, Is.Empty);
    }

    private static string Required(JsonElement element, string property, ICollection<string> violations)
    {
        if (!element.TryGetProperty(property, out var value) || string.IsNullOrWhiteSpace(value.GetString()))
        {
            violations.Add($"Required deprecation field '{property}' is missing or empty.");
            return string.Empty;
        }
        return value.GetString()!;
    }

    private static HashSet<string> ReadStringSet(JsonElement array, string property) =>
        array.EnumerateArray()
            .Select(entry => entry.GetProperty(property).GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToHashSet(StringComparer.Ordinal);

    private static IReadOnlyList<RuntimeExport> ScanProductionRuntimeExports(string root)
    {
        var exports = new List<RuntimeExport>();
        foreach (var path in EnumerateProductionSources(root))
        {
            var relativePath = NormalizePath(Path.GetRelativePath(root, path));
            foreach (Match match in RuntimeExportRegex().Matches(File.ReadAllText(path)))
                exports.Add(new RuntimeExport(match.Groups["kind"].Value, match.Groups["alias"].Value, relativePath));
        }
        return exports;
    }

    private static IEnumerable<string> EnumerateProductionSources(string root) =>
        Directory.EnumerateFiles(Path.Combine(root, "UniversalToolchain"), "*.cs", SearchOption.AllDirectories)
            .Where(static path =>
            {
                var normalized = NormalizePath(path);
                return !normalized.Contains("/bin/", StringComparison.Ordinal)
                       && !normalized.Contains("/obj/", StringComparison.Ordinal)
                       && !IsTestSourcePath(normalized)
                       && !normalized.EndsWith(".g.cs", StringComparison.Ordinal)
                       && !normalized.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);
            });

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalToolchain", "Tests", "Tests.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }
        Assert.Fail("Repository root was not found from the test directory.");
        return string.Empty;
    }

    private static string NormalizePath(string path) => path.Replace(Path.DirectorySeparatorChar, '/');

    private static bool IsTestSourcePath(string normalizedPath) =>
        normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(static segment => segment.Equals("Tests", StringComparison.Ordinal)
                                   || segment.EndsWith(".Tests", StringComparison.Ordinal));

    [GeneratedRegex("DialectRuntimeExport\\(\\\"(?<kind>FrontendModule|Optimizer|Backend)\\\"\\s*,\\s*\\\"(?<alias>[^\\\"]+)\\\"", RegexOptions.CultureInvariant)]
    private static partial Regex RuntimeExportRegex();

    private sealed record RuntimeExport(string Kind, string Alias, string RelativePath);
}
