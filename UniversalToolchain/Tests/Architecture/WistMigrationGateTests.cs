using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalToolchain.Dialects.Wist.Presets;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Wist.LanguagePack;

namespace Tests.Architecture;

[TestFixture]
public sealed partial class WistMigrationGateTests
{
    [Test]
    public void ParityMatrix_MapsTypedFeaturesAndInfrastructureOwners()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "WIST_PARITY_MATRIX.json")));
        var matrix = document.RootElement;
        var descriptor = new WistLanguageFeaturePackage().Descriptor;
        var violations = new List<string>();

        Assert.Multiple(() =>
        {
            Assert.That(
                matrix.GetProperty("genericPackPositioning").GetString(),
                Is.EqualTo("Canonical typed Wist authoring package over the canonical Wist dialect runtime"));
            Assert.That(matrix.GetProperty("replacementClaimAllowed").GetBoolean(), Is.True);
            Assert.That(
                matrix.GetProperty("replacementGate").GetProperty("scope").GetString(),
                Is.EqualTo("shipped presets and typed runtime selection"));
        });

        var featureById = descriptor.Features.ToDictionary(static feature => feature.Id);
        var contributionById = descriptor.Contributions.ToDictionary(static contribution => contribution.Id);
        var typedModuleAliases = descriptor.Contributions
            .Where(static contribution => contribution.Slot == LanguageSlots.FrontendSyntax)
            .Select(static contribution => contribution.Metadata.GetValueOrDefault("wist.moduleAlias"))
            .Where(static alias => !string.IsNullOrWhiteSpace(alias))
            .Select(static alias => alias!)
            .ToHashSet(StringComparer.Ordinal);
        var typedOptimizerAliases = descriptor.Contributions
            .Where(static contribution => contribution.Slot == LanguageSlots.Optimizers)
            .Select(static contribution => contribution.Metadata.GetValueOrDefault("wist.optimizerAlias"))
            .Where(static alias => !string.IsNullOrWhiteSpace(alias))
            .Select(static alias => alias!)
            .ToHashSet(StringComparer.Ordinal);

        var matrixModuleAliases = ReadStringSet(matrix.GetProperty("modules"), "runtimeAlias");
        var matrixInfrastructureAliases = ReadStringSet(matrix.GetProperty("infrastructureModules"), "runtimeAlias");
        var matrixOptimizerAliases = ReadStringSet(matrix.GetProperty("optimizers"), "runtimeAlias");
        var matrixBackendIds = ReadStringSet(matrix.GetProperty("backends"), "typedBackendId");

        if (!matrixModuleAliases.SetEquals(typedModuleAliases))
            violations.Add("Parity matrix module aliases differ from typed Wist module contributions.");
        if (!matrixOptimizerAliases.SetEquals(typedOptimizerAliases))
            violations.Add("Parity matrix optimizer aliases differ from typed Wist optimizer contributions.");
        if (!matrixBackendIds.SetEquals(new WistLanguageRuntimeProvider().SupportedBackends.Select(static backend => backend.Value)))
            violations.Add("Parity matrix backend IDs differ from the Wist runtime provider.");

        ValidateFeatureMappings(
            matrix.GetProperty("modules"),
            "wist.moduleAlias",
            featureById,
            contributionById,
            violations);
        ValidateFeatureMappings(
            matrix.GetProperty("optimizers"),
            "wist.optimizerAlias",
            featureById,
            contributionById,
            violations);

        var exports = ScanProductionRuntimeExports(root);
        var expectedInfrastructureAliases = exports
            .Where(static export => export.Kind == "FrontendModule")
            .Select(static export => export.Alias)
            .Where(alias => !typedModuleAliases.Contains(alias))
            .ToHashSet(StringComparer.Ordinal);
        if (!matrixInfrastructureAliases.SetEquals(expectedInfrastructureAliases))
            violations.Add("Parity matrix infrastructure modules differ from non-selectable production runtime exports.");

        foreach (var entry in matrix.GetProperty("infrastructureModules").EnumerateArray())
        {
            if (string.IsNullOrWhiteSpace(entry.GetProperty("typedOwner").GetString()))
                violations.Add($"Infrastructure module '{entry.GetProperty("runtimeAlias").GetString()}' has no typed owner.");
        }

        foreach (var export in exports)
        {
            var covered = export.Kind switch
            {
                "FrontendModule" => matrixModuleAliases.Contains(export.Alias) || matrixInfrastructureAliases.Contains(export.Alias),
                "Optimizer" => matrixOptimizerAliases.Contains(export.Alias),
                "Backend" => matrixBackendIds.Contains(export.Alias),
                _ => true
            };
            if (!covered)
                violations.Add($"{export.Kind} '{export.Alias}' from {export.RelativePath} is missing from WIST_PARITY_MATRIX.json");
        }

        var matrixPresetIds = ReadStringSet(matrix.GetProperty("presets"), "id");
        var shippedPresetIds = WistShippedDialectPresets.All.Select(static x => x.Id).ToHashSet(StringComparer.Ordinal);
        var typedPresetIds = WistLanguageDefinitions.PresetIds.ToHashSet(StringComparer.Ordinal);
        if (!matrixPresetIds.SetEquals(shippedPresetIds))
            violations.Add("Parity matrix preset IDs differ from shipped dialect presets.");
        if (!matrixPresetIds.SetEquals(typedPresetIds))
            violations.Add("Parity matrix preset IDs differ from typed Wist definitions.");

        var availableEvidenceTests = ScanNUnitTestNames(root);
        var allowedStatuses = ReadStringSet(matrix.GetProperty("statusVocabulary"));
        var expectedStatuses = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["modules"] = "SelectionEquivalent",
            ["infrastructureModules"] = "InfrastructureEquivalent",
            ["optimizers"] = "SelectionEquivalent",
            ["backends"] = "ExecutableEquivalent",
            ["presets"] = "ExecutableEquivalent"
        };
        foreach (var (sectionName, expectedStatus) in expectedStatuses)
        {
            foreach (var entry in matrix.GetProperty(sectionName).EnumerateArray())
            {
                var status = entry.GetProperty("status").GetString();
                if (status == null || !allowedStatuses.Contains(status))
                    violations.Add($"{sectionName} contains an unknown status '{status}'.");
                if (status != expectedStatus)
                    violations.Add($"{sectionName} must use status '{expectedStatus}', not '{status}'.");
                var evidenceTests = entry.GetProperty("tests")
                    .EnumerateArray()
                    .Select(static test => test.GetString())
                    .Where(static test => !string.IsNullOrWhiteSpace(test))
                    .Select(static test => test!)
                    .ToArray();
                if (evidenceTests.Length == 0)
                {
                    violations.Add($"{sectionName} contains an entry without evidence references.");
                }
                foreach (var evidenceTest in evidenceTests)
                {
                    if (!availableEvidenceTests.Contains(evidenceTest))
                        violations.Add($"{sectionName} references missing NUnit evidence test '{evidenceTest}'.");
                }
            }
        }

        Assert.That(violations, Is.Empty);
    }

    private static void ValidateFeatureMappings(
        JsonElement entries,
        string aliasMetadataKey,
        IReadOnlyDictionary<LanguageFeatureId, LanguageFeatureDescriptor> featureById,
        IReadOnlyDictionary<LanguageContributionId, LanguageContributionDescriptor> contributionById,
        ICollection<string> violations)
    {
        foreach (var entry in entries.EnumerateArray())
        {
            var runtimeAlias = entry.GetProperty("runtimeAlias").GetString()!;
            var featureId = new LanguageFeatureId(entry.GetProperty("typedFeatureId").GetString()!);
            if (!featureById.TryGetValue(featureId, out var feature))
            {
                violations.Add($"Runtime alias '{runtimeAlias}' references missing typed feature '{featureId.Value}'.");
                continue;
            }

            var mappedAliases = feature.Contributions
                .Where(contributionById.ContainsKey)
                .Select(contributionId => contributionById[contributionId].Metadata.GetValueOrDefault(aliasMetadataKey))
                .Where(static alias => !string.IsNullOrWhiteSpace(alias))
                .ToHashSet(StringComparer.Ordinal);
            if (!mappedAliases.Contains(runtimeAlias))
            {
                violations.Add(
                    $"Typed feature '{featureId.Value}' does not select runtime alias '{runtimeAlias}' through '{aliasMetadataKey}'.");
            }
        }
    }

    [Test]
    public void RemovedLegacySurface_CannotReturn()
    {
        var root = FindRepositoryRoot();
        var registryPath = Path.Combine(root, "eng", "retired-surface.json");
        using var document = JsonDocument.Parse(File.ReadAllText(registryPath));
        var registry = document.RootElement;
        Assert.That(registry.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));

        var violations = new List<string>();
        foreach (var entry in registry.GetProperty("paths").EnumerateArray())
        {
            var relativePath = entry.GetString()!;
            var absolutePath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absolutePath) || Directory.Exists(absolutePath))
                violations.Add($"Retired path returned: {relativePath}");
        }

        var prohibitedPatterns = registry.GetProperty("symbols")
            .EnumerateArray()
            .Select(static entry => (
                Name: entry.GetProperty("name").GetString()!,
                Pattern: new Regex(entry.GetProperty("pattern").GetString()!, RegexOptions.CultureInvariant)))
            .ToArray();

        foreach (var path in EnumerateProductionSources(root))
        {
            var source = File.ReadAllText(path);
            foreach (var (name, pattern) in prohibitedPatterns)
            {
                if (pattern.IsMatch(source))
                    violations.Add($"{name} found in {NormalizePath(Path.GetRelativePath(root, path))}");
            }
        }

        Assert.That(violations, Is.Empty);
    }

    private static HashSet<string> ReadStringSet(JsonElement array, string property) =>
        array.EnumerateArray()
            .Select(entry => entry.GetProperty(property).GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> ReadStringSet(JsonElement array) =>
        array.EnumerateArray()
            .Select(static entry => entry.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> ScanNUnitTestNames(string root)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "UniversalToolchain"), "*.cs", SearchOption.AllDirectories))
        {
            var normalized = NormalizePath(path);
            if (normalized.Contains("/bin/", StringComparison.Ordinal)
                || normalized.Contains("/obj/", StringComparison.Ordinal)
                || !IsTestSourcePath(normalized))
            {
                continue;
            }

            var rootNode = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
            foreach (var method in rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var isNUnitTest = method.AttributeLists
                    .SelectMany(static list => list.Attributes)
                    .Select(static attribute => attribute.Name.ToString())
                    .Any(static name => name is "Test" or "TestCase" or "TestCaseSource"
                                         || name.EndsWith(".Test", StringComparison.Ordinal)
                                         || name.EndsWith(".TestCase", StringComparison.Ordinal)
                                         || name.EndsWith(".TestCaseSource", StringComparison.Ordinal));
                if (isNUnitTest)
                    names.Add(method.Identifier.ValueText);
            }
        }
        return names;
    }

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
                       && !normalized.Contains("/Experiments/", StringComparison.Ordinal)
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
