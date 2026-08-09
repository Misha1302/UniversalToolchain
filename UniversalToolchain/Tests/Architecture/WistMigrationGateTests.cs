using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Wist.LanguagePack;

namespace Tests.Architecture;

[TestFixture]
public sealed class WistMigrationGateTests
{
    [Test]
    public void DialectDslFrontend_CannotReintroduceLegacyPlanning()
    {
        var root = FindRepositoryRoot();
        var matrixPath = Path.Combine(root, "WIST_PARITY_MATRIX.json");
        var matrixText = File.ReadAllText(matrixPath);
        using var document = JsonDocument.Parse(matrixText);
        var matrix = document.RootElement;
        var contracts = matrix.GetProperty("artifactContracts");

        var matrixBackends = matrix.GetProperty("backends")
            .EnumerateArray()
            .Select(static entry => entry.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToHashSet(StringComparer.Ordinal);
        var packageBackends = new WistLanguageFeaturePackage().Descriptor.Contributions
            .Where(static contribution => contribution.Slot == LanguageSlots.Backends)
            .SelectMany(static contribution => contribution.SupportedBackends)
            .Select(static backend => backend.Value)
            .ToHashSet(StringComparer.Ordinal);
        var matrixPresets = matrix.GetProperty("presets")
            .EnumerateArray()
            .Select(static entry => entry.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToHashSet(StringComparer.Ordinal);
        var typedPresets = WistLanguageDefinitions.PresetIds.ToHashSet(StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(matrix.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(3));
            Assert.That(
                matrix.GetProperty("positioning").GetString(),
                Is.EqualTo("LanguageDefinition -> LanguageCompiler -> LanguagePlan -> LanguageRuntime"));
            Assert.That(matrix.GetProperty("parityBasis").GetString(), Is.EqualTo("typed-semantic-projection"));
            Assert.That(matrixBackends, Is.EquivalentTo(packageBackends));
            Assert.That(matrixPresets, Is.EquivalentTo(typedPresets));
            Assert.That(contracts.GetProperty("syntax").GetString(), Is.EqualTo(WistArtifactKinds.SyntaxTreeContract.ValueTypeIdentity));
            Assert.That(contracts.GetProperty("bytecode").GetString(), Is.EqualTo(WistArtifactKinds.BytecodeContract.ValueTypeIdentity));
            Assert.That(contracts.GetProperty("air").GetString(), Is.EqualTo(WistArtifactKinds.AirContract.ValueTypeIdentity));
            Assert.That(contracts.GetProperty("cil").GetString(), Is.EqualTo(WistArtifactKinds.CilArtifactContract.ValueTypeIdentity));
            Assert.That(contracts.GetProperty("interpreter").GetString(), Is.EqualTo(WistArtifactKinds.InterpreterArtifactContract.ValueTypeIdentity));
            Assert.That(matrixText, Does.Not.Contain("runtimeAlias"));
            Assert.That(matrixText, Does.Not.Contain("canonical Wist dialect runtime"));
            Assert.That(matrixText, Does.Not.Contain("SelectedRuntimePlan"));
            Assert.That(matrixText, Does.Not.Contain("DialectBuildPlan"));
        });

        var compilerSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalToolchain",
            "UniversalToolchain.Dialects.Frontend",
            "DialectDslCompiler.cs"));
        var translatorSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalToolchain",
            "UniversalToolchain.Wist.LanguagePack",
            "WistFacadeLanguageDefinitionFactory.cs"));
        var forbidden = new[]
        {
            "BasicCoreImpl",
            "DialectBuildPlan",
            "SelectedRuntimePlan",
            "SelectedRuntimePlanResolver",
            "ToolchainCompositionWorkflow",
            "WistDialectExecutionWorkflow",
            "WistLanguageRuntimeProvider"
        };
        foreach (var symbol in forbidden)
        {
            Assert.Multiple(() =>
            {
                Assert.That(compilerSource, Does.Not.Contain(symbol), $"DialectDslCompiler must not reference {symbol}.");
                Assert.That(translatorSource, Does.Not.Contain(symbol), $"Wist dialect translator must not reference {symbol}.");
            });
        }
    }

    [Test]
    public void CanonicalProjects_DoNotDependOnLegacyWistRuntimeProject()
    {
        var root = FindRepositoryRoot();
        string[] canonicalProjects =
        [
            "UniversalToolchain/UniversalToolchain.Wist/UniversalToolchain.Wist.csproj",
            "UniversalToolchain/UniversalToolchain.Wist.LanguagePack/UniversalToolchain.Wist.LanguagePack.csproj",
            "UniversalToolchain/Wistc/Wistc.csproj",
            "UniversalToolchain/Example/Example.csproj"
        ];

        var violations = canonicalProjects
            .Where(project => File.ReadAllText(Path.Combine(root, project.Replace('/', Path.DirectorySeparatorChar)))
                .Contains("UniversalToolchain.Dialects.Wist", StringComparison.Ordinal))
            .Select(static project => $"Canonical project still depends on legacy Wist runtime project: {project}")
            .ToArray();

        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
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

        string[] canonicalDocs =
        [
            "docs/current-canonical-runtime-pipeline.md",
            "docs/build-dsls/dialect-files.md",
            "docs/reference/dialect-reference.md",
            "docs/dialect-groups.md",
            "docs/start/what-is-wist.md",
            "docs/internals/dependency-injection.md",
            "docs/CONTRIBUTING.md",
            "docs/write-modules/create-your-first-module.md",
            "docs/write-modules/runtime-manifests.md",
            "docs/runtime-manifest-activation-model.md",
            "docs/runtime-manifest-format.md",
            "UniversalToolchain/Dialects/examples/wist/minimal-arithmetic-grouped/README.md",
            "internal-docs/policies-and-reports/public-claim-ledger.md",
            "internal-docs/policies-and-reports/technical-debt.md"
        ];
        string[] staleCurrentClaims =
        [
            "Runtime manifests are how dialect composition discovers selectable runtime components",
            "The source of truth should be the module/export attributes plus generated manifest output",
            "is the main orchestration object for dialect execution",
            "source of truth for public Wist dialect execution is the manifest-backed runtime workflow",
            "Keep the canonical dialect path deterministic: dialect compilation, build-plan construction, manifest-backed runtime",
            "selected dialect is compiled into a build plan, resolved against runtime manifests"
        ];

        foreach (var relativePath in canonicalDocs)
        {
            var text = File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!text.Contains("LanguageCompiler", StringComparison.Ordinal))
                violations.Add($"Canonical documentation does not name LanguageCompiler: {relativePath}");
            if (!text.Contains("LanguagePlan", StringComparison.Ordinal))
                violations.Add($"Canonical documentation does not name LanguagePlan: {relativePath}");
            foreach (var staleClaim in staleCurrentClaims)
            {
                if (text.Contains(staleClaim, StringComparison.OrdinalIgnoreCase))
                    violations.Add($"Retired Wist runtime claim remains current in {relativePath}: {staleClaim}");
            }
        }

        var claimLedger = File.ReadAllText(Path.Combine(
            root,
            "internal-docs",
            "policies-and-reports",
            "public-claim-ledger.md"));
        if (!claimLedger.Contains("| Wist runtime selection is `LanguagePlan`-backed |", StringComparison.Ordinal))
            violations.Add("Public claim ledger no longer pins Wist runtime selection to LanguagePlan.");
        if (claimLedger.Contains("| Runtime selection is manifest-backed |", StringComparison.Ordinal))
            violations.Add("Public claim ledger restored manifest-backed Wist runtime selection as an active claim.");

        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
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
}
