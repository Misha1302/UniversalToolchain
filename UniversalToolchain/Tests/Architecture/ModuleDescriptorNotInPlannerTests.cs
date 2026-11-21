using System.Text.RegularExpressions;

namespace Tests.Architecture;

/// <summary>
/// Enforces ADR-008: modules are descriptive, never a planner concept.
/// <para>
/// The descriptor exists so that lowering dispatch and diagnostics can name the mechanism that owns
/// a contribution. The moment the planner references it, "module" becomes a second selection layer
/// beside features and contributions — the exact shape ADR-008 rejects. Because that change would
/// still compile and still produce plausible-looking plans, review alone is not a sufficient guard;
/// the dependency direction is asserted mechanically here.
/// </para>
/// </summary>
[TestFixture]
public sealed class ModuleDescriptorNotInPlannerTests
{
    [Test]
    public void LanguageSdk_DoesNotReferenceModuleDescriptors()
    {
        var root = FindRepositoryRoot();
        var plannerDirectory = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.LanguageSdk");
        var offenders = Directory
            .GetFiles(plannerDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(IsProductionFile)
            .Where(path => File.ReadAllText(path).Contains("LanguageModuleDescriptor", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToList();

        Assert.That(offenders, Is.Empty,
            "UniversalToolchain.LanguageSdk is the single planner and must not depend on module " +
            "descriptors (ADR-008). Modules describe what the planner already selected; letting the " +
            "planner read them creates a second selection layer. Offenders: "
            + string.Join(", ", offenders));
    }

    [Test]
    public void ModuleRegistry_ExposesNoSelectionSurface()
    {
        // A registry that could answer "which contributions should be enabled for these features"
        // would be a second planner wearing a different name. The public surface is deliberately
        // limited to ownership lookup, so the declarations are enumerated and compared by name
        // rather than by scanning the bodies (which legitimately use LINQ Select).
        var root = FindRepositoryRoot();
        var registry = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.FeatureSdk", "LanguageModuleRegistry.cs");
        var text = File.ReadAllText(registry);
        var declared = Regex.Matches(text, @"public\s+(?:sealed\s+)?[\w<>,\[\]?.\s]+?\s+(\w+)\s*\(")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.That(declared, Is.EquivalentTo(new[] { "Add", "AddRange", "TryGetOwner" }),
            "LanguageModuleRegistry must expose ownership lookup only. A selection-shaped public method " +
            $"would make modules a planning authority. Declared: {string.Join(", ", declared.OrderBy(static x => x, StringComparer.Ordinal))}");
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
