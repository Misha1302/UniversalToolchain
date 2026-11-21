using System.Text.RegularExpressions;

namespace Tests.Architecture;

/// <summary>
/// Enforces ADR-001: exactly one planner, and runtime never plans.
/// <para>
/// <c>LanguageCompiler</c> resolves features, resolves contributions, and selects an artifact route.
/// Those three phases are <c>internal</c> to <c>UniversalToolchain.LanguageSdk</c>, and
/// <c>UniversalToolchain.Runtime</c> executes a plan it is handed. The whole point is that a runtime
/// cannot silently re-plan: if it could reach the resolution phases, a second, always-active authority
/// would exist beside the compiler, and the two could disagree about what was selected.
/// </para>
/// <para>
/// "It is internal, so it is unreachable" is an assumption that a single <c>InternalsVisibleTo</c> or a
/// visibility edit would invalidate without breaking any test. The three assertions below check the
/// fact instead: the phase declarations are internal, the SDK grants no internals, and the runtime
/// assembly names no phase type and no feature type.
/// </para>
/// <para>
/// <see cref="GateDetectsEachBypass"/> mutates the analysed text and requires the analysis to report a
/// violation for each shape, because a source-scan gate that cannot fail is a green blank.
/// </para>
/// </summary>
[TestFixture]
public sealed class SinglePlannerAuthorityTests
{
    private static readonly string[] PhaseTypes =
    [
        "LanguageFeatureResolutionPhase",
        "LanguageContributionResolutionPhase",
        "LanguageArtifactRoutePhase"
    ];

    [Test]
    public void ResolutionPhases_AreInternalAndNotPublic()
    {
        var root = FindRepositoryRoot();
        var offenders = new List<string>();

        foreach (var phase in PhaseTypes)
        {
            var declaration = FindDeclaration(root, phase);
            if (declaration == null)
            {
                offenders.Add($"{phase}: declaration not found; the gate cannot verify a type it cannot locate");
                continue;
            }

            if (!Regex.IsMatch(declaration, @"\binternal\b"))
                offenders.Add($"{phase}: not declared internal -> {declaration.Trim()}");
            if (Regex.IsMatch(declaration, @"\bpublic\b"))
                offenders.Add($"{phase}: declared public -> {declaration.Trim()}");
        }

        Assert.That(offenders, Is.Empty,
            "the three planning phases must stay internal to the SDK: " + string.Join("; ", offenders));
    }

    [Test]
    public void LanguageSdk_GrantsNoInternalsToAnyAssembly()
    {
        // The phases are internal, and that is the enforcement. An InternalsVisibleTo attribute would
        // hand the phases to another assembly while leaving every declaration looking correct.
        var root = FindRepositoryRoot();
        var sdk = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.LanguageSdk");
        var offenders = Directory
            .EnumerateFiles(sdk, "*.cs", SearchOption.AllDirectories)
            .Where(IsProductionFile)
            .SelectMany(path => File
                .ReadAllLines(path)
                .Where(line => line.Contains("InternalsVisibleTo", StringComparison.Ordinal))
                .Select(line => $"{Path.GetFileName(path)}: {line.Trim()}"))
            .ToList();

        Assert.That(offenders, Is.Empty,
            "UniversalToolchain.LanguageSdk must not expose its internals: a granted InternalsVisibleTo " +
            "would let another assembly drive the resolution phases directly, creating the second " +
            "planning authority ADR-001 forbids. Offenders: " + string.Join("; ", offenders));
    }

    [Test]
    public void Runtime_ReferencesNoResolutionPhaseType()
    {
        var root = FindRepositoryRoot();
        var violations = AnalyzeReferenceText(
            string.Join(
                "\n",
                Directory
                    .EnumerateFiles(Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Runtime"), "*.cs", SearchOption.AllDirectories)
                    .Where(IsProductionFile)
                    .Select(File.ReadAllText)),
            PhaseTypes);

        Assert.That(violations, Is.Empty,
            "UniversalToolchain.Runtime executes plans; it must never reference a resolution phase. " +
            "A reference here means the runtime can re-decide selection at execution time. " +
            string.Join("; ", violations));
    }

    [Test]
    public void Runtime_ReferencesNoPlannerFeatureType()
    {
        // Features are a planning concept. A runtime that touches LanguageFeatureId is reasoning about
        // feature selection, which is the compiler's job.
        var root = FindRepositoryRoot();
        var text = string.Join(
            "\n",
            Directory
                .EnumerateFiles(Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Runtime"), "*.cs", SearchOption.AllDirectories)
                .Where(IsProductionFile)
                .Select(File.ReadAllText));

        var violations = AnalyzeReferenceText(text, ["LanguageFeatureId", "LanguageFeatureResolutionPhase"]);
        Assert.That(violations, Is.Empty,
            "UniversalToolchain.Runtime must not reason about features: " + string.Join("; ", violations));
    }

    [Test]
    public void GateDetectsEachBypass()
    {
        // The mutant self-test. Each mutation is the smallest edit that would create a second planning
        // authority; the analysis must report it.
        var cases = new (string Name, string Text, string[] Symbols)[]
        {
            ("phase made public", "public static class LanguageArtifactRoutePhase { }", PhaseTypes),
            ("runtime calls a phase", "LanguageArtifactRoutePhase.Run(plan);", PhaseTypes),
            ("runtime reads a plan contribution resolution", "LanguageContributionResolutionPhase.Resolve(definition);", PhaseTypes),
            ("runtime reasons about features", "foreach (var feature in LanguageFeatureId.Create(\"x\")) { }", ["LanguageFeatureId"])
        };

        var survivors = new List<string>();
        foreach (var (name, text, symbols) in cases)
        {
            var violations = AnalyzeReferenceText(text, symbols);
            var declaresPublic = Regex.IsMatch(text, @"\bpublic\b") && symbols.All(s => text.Contains(s, StringComparison.Ordinal));

            // Two distinct analyses are exercised: reference detection, and public-declaration detection.
            if (violations.Count == 0 && !declaresPublic)
                survivors.Add(name);
        }

        Assert.That(survivors, Is.Empty,
            "the single-planner gate failed to kill these mutations, so it cannot enforce ADR-001: " +
            string.Join(", ", survivors));
    }

    /// <summary>
    /// Reports references to planning types, ignoring comments and XML documentation.
    /// </summary>
    private static List<string> AnalyzeReferenceText(string text, IReadOnlyList<string> symbols)
    {
        var violations = new List<string>();
        var lines = text.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var code = StripComment(lines[index]);
            if (code.Trim().Length == 0)
                continue;

            foreach (var symbol in symbols)
            {
                // Word boundary matters: `LanguageArtifactRoutePhase` must not match `...Phases`.
                if (Regex.IsMatch(code, $@"\b{Regex.Escape(symbol)}\b"))
                    violations.Add($"line {index + 1}: {symbol} in `{code.Trim()}`");
            }
        }

        return violations;
    }

    private static string StripComment(string line)
    {
        // Good enough for a source scan: the gate is looking for code references, and a false positive
        // from a string literal is still a review question worth answering.
        var comment = line.IndexOf("//", StringComparison.Ordinal);
        return comment < 0 ? line : line[..comment];
    }

    private static string? FindDeclaration(string root, string typeName)
    {
        var sdk = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.LanguageSdk");
        var pattern = new Regex($@"^.*\bclass\s+{Regex.Escape(typeName)}\b.*$", RegexOptions.Multiline);
        foreach (var path in Directory.EnumerateFiles(sdk, "*.cs", SearchOption.AllDirectories).Where(IsProductionFile))
        {
            var match = pattern.Match(File.ReadAllText(path));
            if (match.Success)
                return match.Value;
        }

        return null;
    }

    private static bool IsProductionFile(string path) =>
        !Normalize(path).Contains("/bin/", StringComparison.Ordinal) &&
        !Normalize(path).Contains("/obj/", StringComparison.Ordinal);

    private static string Normalize(string path) => path.Replace(Path.DirectorySeparatorChar, '/');

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalToolchain", "Wist.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root with UniversalToolchain/Wist.sln was not found.");
    }
}
