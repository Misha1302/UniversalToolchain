using System.Text.RegularExpressions;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// UT-A047: backends are described, not branched on by name.
/// <para>
/// Before this change, backend identity was a string compared against <c>"cil"</c> or
/// <c>"interpreter"</c> in five places, three of which ended in
/// <c>throw new InvalidOperationException("Unsupported Wist backend '...'")</c>. Adding a third backend
/// meant finding all of them, and nothing failed at build time if you missed one: the default arm of a
/// string switch is only wrong when it runs.
/// </para>
/// <para>
/// The claim "no name branching remains" cannot be checked by reading the diff, because the next commit
/// can reintroduce it. The gate below scans the pack source. <see cref="GateDetectsEachBypass"/> mutates
/// the analysed text and requires a violation for each shape, so the scan is proven able to fail.
/// </para>
/// </summary>
[TestFixture]
public sealed class BackendDescriptorTests
{
    private static readonly string[] BackendNames = ["\"cil\"", "\"interpreter\""];

    /// <summary>Files allowed to name a backend: identity declarations and the descriptor table.</summary>
    private static readonly string[] NameDeclarationFiles =
    [
        "WistBackends.cs",
        "WistLanguageFeaturePackage.cs",
        "WistLanguageDefinitions.cs",
        "WistFacadeLanguageDefinitionFactory.cs",
        "WistLanguagePackIdentity.cs",
        "WistLanguageRuntimeProvider.cs",
        "WistDirectLanguageRuntimeProvider.cs",
        "WistDirectRuntimeComponents.cs",
        "WistBuiltArtifactActivation.cs",
        "WistDialectGroupCatalog.cs"
    ];

    [Test]
    public void PackHasNoBackendNameComparison()
    {
        var root = PackRoot();
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;

            var name = Path.GetFileName(file);
            var text = File.ReadAllText(file);
            var findings = NameComparisonFindings(text, name);
            offenders.AddRange(findings.Select(finding => $"{name}: {finding}"));
        }

        Assert.That(offenders, Is.Empty,
            "backend identity must be asked of WistBackendCatalog, never compared to a literal: " +
            string.Join(" | ", offenders));
    }

    [Test]
    public void GateDetectsEachBypass()
    {
        // A source scan that cannot fail is a green blank. Each mutant is a shape that existed in this
        // repository before the change, so the gate is proven against the real defect.
        var mutants = new (string Name, string Text)[]
        {
            ("switch-arm", "var x = backend.Value switch { \"cil\" => 1, _ => 0 };"),
            ("equality", "if (backend.Value == \"interpreter\") { }"),
            ("inequality", "if (backend.Value != \"cil\") { }"),
            ("is-pattern", "if (backend.Value is \"cil\") { }"),
            ("equals-call", "if (string.Equals(backend.Value, \"interpreter\", StringComparison.Ordinal)) { }"),
            ("contains", "if (backend.Value.Contains(\"cil\")) { }")
        };

        Assert.Multiple(() =>
        {
            foreach (var (name, text) in mutants)
                Assert.That(NameComparisonFindings(text, "Mutant.cs"), Is.Not.Empty,
                    $"the gate must reject the '{name}' shape, or it cannot protect against it");
        });
    }

    [Test]
    public void GateAcceptsADeclarationButRejectsAComparison()
    {
        // The literal has to exist somewhere: "cil" is an identity, not a decision. A gate that rejected
        // every occurrence would force authors to obfuscate the name, which is worse than the defect.
        Assert.Multiple(() =>
        {
            Assert.That(NameComparisonFindings("public static BackendId Cil { get; } = new(\"cil\");", "WistBackends.cs"),
                Is.Empty, "declaring the identity must stay legal");
            Assert.That(NameComparisonFindings("private static readonly BackendId Cil = new(\"cil\");", "WistDirectRuntimeComponents.cs"),
                Is.Empty, "a private identity field is a declaration, not a comparison");
            Assert.That(NameComparisonFindings("if (backend.Value == \"cil\") { }", "WistModuleSelection.cs"),
                Is.Not.Empty, "comparing the same literal must be rejected");
        });
    }

    [Test]
    public void EveryConfiguredBackend_HasADescriptor()
    {
        // The set of backends the pack ships and the set it can describe must be the same set. A preset
        // enabling a backend with no descriptor would pass planning and fail at build time.
        var configured = UniversalToolchain.Wist.LanguagePack.WistLanguageDefinitions.PresetIds
            .Select(UniversalToolchain.Wist.LanguagePack.WistLanguageDefinitions.Create)
            .SelectMany(static definition => definition.Backends)
            .Select(static backend => backend.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        var described = UniversalToolchain.Wist.LanguagePack.WistBackendCatalog.Canonical.All
            .Select(static descriptor => descriptor.Id.Value)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.That(described, Is.EqualTo(configured),
            "every backend a preset can enable must be described, and every descriptor must correspond to a " +
            "backend the pack actually configures");
    }

    [Test]
    public void Descriptors_AreCompleteAndDistinct()
    {
        var descriptors = UniversalToolchain.Wist.LanguagePack.WistBackendCatalog.Canonical.All;

        Assert.Multiple(() =>
        {
            Assert.That(descriptors, Is.Not.Empty);
            Assert.That(descriptors.Select(static descriptor => descriptor.Id.Value), Is.Unique,
                "two descriptors for one backend would make lookup order decide behaviour");
            Assert.That(descriptors.All(static descriptor => !string.IsNullOrWhiteSpace(descriptor.PipelineComponentId)),
                "a descriptor without a pipeline component cannot contribute capability facets");
            Assert.That(
                descriptors.Select(static descriptor => descriptor.PipelineComponentId).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(descriptors.Count),
                "two backends sharing a pipeline component id would merge their contract facets");
        });
    }

    [Test]
    public void UnknownBackend_IsRejectedWithAMessageNamingWhatIsDescribed()
    {
        // The old code threw "Unsupported Wist backend 'x'" and said nothing else. An author adding a third
        // backend needs to know where descriptors live and what is already described.
        var exception = Assert.Throws<InvalidOperationException>(
            () => UniversalToolchain.Wist.LanguagePack.WistBackendCatalog.Canonical.GetRequiredDescriptor(new BackendId("wasm")));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("wasm"), "the message must name the backend");
            Assert.That(exception.Message, Does.Contain("WistBackends"),
                "the message must say where a descriptor is added");
            Assert.That(exception.Message, Does.Contain("cil").And.Contain("interpreter"),
                "the message must list what is already described");
        });
    }

    [Test]
    public void SupportedIntrinsics_AreStableAndNonEmptyForBothBackends()
    {
        // A descriptor that silently returned an empty intrinsic list would make every intrinsic appear
        // unsupported, and the failure would surface as a confusing optimization refusal.
        Assert.Multiple(() =>
        {
            foreach (var descriptor in UniversalToolchain.Wist.LanguagePack.WistBackendCatalog.Canonical.All)
            {
                Assert.That(descriptor.SupportedIntrinsics, Is.Not.Empty,
                    $"backend '{descriptor.Id.Value}' must declare its intrinsics");
                Assert.That(descriptor.SupportedIntrinsics, Is.Unique,
                    $"backend '{descriptor.Id.Value}' intrinsic list must not contain duplicates");
                // The shipped CIL list is NOT ordinal-sorted; asserting that would be inventing a property
                // the backend never promised. What matters is that two reads of the same descriptor agree,
                // because observation hashes the intrinsic set.
                var again = descriptor.SupportedIntrinsics.ToArray();
                Assert.That(again, Is.EqualTo(descriptor.SupportedIntrinsics).AsCollection,
                    $"backend '{descriptor.Id.Value}' intrinsic order must be stable across reads");
            }
        });
    }

    /// <summary>
    /// Finds backend-name comparisons. A bare declaration such as <c>new("cil")</c> is identity and is
    /// allowed; a comparison, pattern, switch arm or string call on that literal is a behavioural branch.
    /// </summary>
    internal static IReadOnlyList<string> NameComparisonFindings(string text, string fileName)
    {
        var findings = new List<string>();
        var declared = NameDeclarationFiles.Contains(fileName, StringComparer.Ordinal);
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith("///", StringComparison.Ordinal))
                continue;

            foreach (var name in BackendNames)
            {
                if (!line.Contains(name, StringComparison.Ordinal))
                    continue;

                // Declaration forms: `new("cil")`, `== new("cil")` is still a comparison, so only the
                // constructor form and a field/property initializer count as declarations.
                var isDeclaration = Regex.IsMatch(line, $@"new\s*\(\s*{Regex.Escape(name)}\s*\)") &&
                                    !Regex.IsMatch(line, $@"[=!]=\s*new\s*\(\s*{Regex.Escape(name)}\s*\)");

                var isComparison = Regex.IsMatch(line, $@"[=!]=\s*{Regex.Escape(name)}") ||
                                   Regex.IsMatch(line, $@"{Regex.Escape(name)}\s*[=!]=") ||
                                   Regex.IsMatch(line, $@"\bis\s+{Regex.Escape(name)}") ||
                                   Regex.IsMatch(line, $@"\bcase\s+{Regex.Escape(name)}") ||
                                   Regex.IsMatch(line, $@"{Regex.Escape(name)}\s*=>") ||
                                   Regex.IsMatch(line, $@"\b(?:Equals|Contains|StartsWith|EndsWith)\s*\([^)]*{Regex.Escape(name)}");

                if (isComparison)
                {
                    findings.Add($"backend-name comparison -> {trimmed}");
                    continue;
                }

                if (!isDeclaration && !declared)
                    findings.Add($"backend name outside a declaration file -> {trimmed}");
            }
        }
        return findings;
    }

    private static string PackRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "UniversalToolchain", "Wist.sln")))
            directory = directory.Parent;
        Assert.That(directory, Is.Not.Null, "repository root not found");
        return Path.Combine(directory!.FullName, "UniversalToolchain", "UniversalToolchain.Wist.LanguagePack");
    }
}
