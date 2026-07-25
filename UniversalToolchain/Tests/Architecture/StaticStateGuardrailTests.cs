using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tests.Architecture;

[TestFixture]
public sealed class StaticStateGuardrailTests
{
    private static readonly HashSet<string> MutableCollectionTypeNames =
    [
        "ArrayList",
        "ConcurrentBag",
        "ConcurrentDictionary",
        "ConcurrentQueue",
        "ConcurrentStack",
        "Dictionary",
        "HashSet",
        "Hashtable",
        "LinkedList",
        "List",
        "ObservableCollection",
        "OrderedDictionary",
        "Queue",
        "SortedDictionary",
        "SortedList",
        "SortedSet",
        "Stack"
    ];

    [Test]
    public void ProductionSources_ShouldNotContainUnownedMutableStaticCollectionFields()
    {
        var root = FindRepositoryRoot();
        var exceptions = LoadExceptions(root);
        var findings = ScanProductionSources(root).ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var violations = new List<string>();

        foreach (var finding in findings)
        {
            if (!exceptions.TryGetValue(finding.Identity, out var exception))
            {
                violations.Add($"{finding.Identity}: mutable static collection has no owner/reason/expiry exception");
                continue;
            }

            ValidateException(exception, finding.Identity, today, violations);
        }

        foreach (var exception in exceptions.Values)
        {
            if (findings.All(finding => !StringComparer.Ordinal.Equals(finding.Identity, exception.Identity)))
                violations.Add($"{exception.Identity}: stale exception no longer matches a mutable static collection field");
        }

        Assert.That(
            violations,
            Is.Empty,
            "Mutable process-wide collections require a reviewed, owned and expiring exception. Prefer immutable/frozen data or instance-scoped state.");
    }

    [Test]
    public void Analyzer_ShouldRecognizeMutableStaticField_AndIgnoreLocalOrImmutableState()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Collections.Immutable;
            internal sealed class Sample
            {
                private static readonly Dictionary<string, int> Shared = new();
                private static readonly ImmutableDictionary<string, int> Frozen = ImmutableDictionary<string, int>.Empty;
                private readonly List<int> Instance = new();
                private static List<int> Build() => new();
            }
            """;

        var findings = ScanSource("Sample.cs", source).ToList();

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Identity, Is.EqualTo("Sample.cs::Sample.Shared"));
    }

    [Test]
    public void ExceptionValidation_ShouldRejectExpiredOrIncompleteEntries()
    {
        var violations = new List<string>();
        var today = new DateOnly(2026, 7, 25);

        ValidateException(
            new StaticStateException("a.cs::A.X", "", "legacy", new DateOnly(2026, 7, 24)),
            "a.cs::A.X",
            today,
            violations);

        Assert.Multiple(() =>
        {
            Assert.That(violations, Has.Some.Contains("owner"));
            Assert.That(violations, Has.Some.Contains("expired"));
        });
    }

    [Test]
    public void DiscoveryAndDynamicMethodLifetimeBoundaries_ShouldRemainProcessLocalAndExplicit()
    {
        var root = FindRepositoryRoot();
        var relevantFiles = new[]
        {
            "UniversalToolchain/AssemblyFinder/ImmutableTypeCatalog.cs",
            "UniversalToolchain/AssemblyFinder/TypeCatalogFactory.cs",
            "UniversalToolchain/DynamicMethodCalling/Core/DynamicMethodInvokerBase.cs",
            "UniversalToolchain/ObjectExtensions/ObjectExtension.cs"
        };

        var source = string.Join(
            Environment.NewLine,
            relevantFiles.Select(path => File.ReadAllText(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)))));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Not.Contain("AppDomain.CurrentDomain"));
            Assert.That(source, Does.Not.Contain("AppContext.BaseDirectory"));
            Assert.That(source, Does.Not.Contain("SearchOption.AllDirectories"));
            Assert.That(source, Does.Not.Contain("LoadFromAssemblyPath"));
            Assert.That(source, Does.Not.Contain("MakeImmortal"));
            Assert.That(source, Does.Not.Contain("_immortalObjects"));
        });
    }

    [Test]
    public void VendoredPackageFeed_ShouldContainOnlyCanonicalRootNugetPackages()
    {
        var root = FindRepositoryRoot();
        var packageRoot = Path.Combine(root, "UniversalToolchain", "packages");
        var files = Directory.Exists(packageRoot)
            ? Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();

        var violations = files
            .Select(path => NormalizePath(Path.GetRelativePath(packageRoot, path)))
            .Where(relativePath => relativePath.Contains('/', StringComparison.Ordinal)
                                   || !relativePath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.That(
            violations,
            Is.Empty,
            "The vendored feed must contain only canonical root-level .nupkg files; cache artifacts and opaque extensions are forbidden.");
    }

    private static IEnumerable<StaticFieldFinding> ScanProductionSources(string root)
    {
        var productionRoot = Path.Combine(root, "UniversalToolchain");

        return Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => IsProductionSource(path))
            .SelectMany(path => ScanSource(NormalizePath(Path.GetRelativePath(root, path)), File.ReadAllText(path)));
    }

    private static IEnumerable<StaticFieldFinding> ScanSource(string relativePath, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp14));
        var root = tree.GetRoot();

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            if (!field.Modifiers.Any(SyntaxKind.StaticKeyword) || !IsMutableCollectionType(field.Declaration.Type))
                continue;

            var owner = field.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ?? "<global>";
            foreach (var variable in field.Declaration.Variables)
                yield return new StaticFieldFinding(relativePath, owner, variable.Identifier.ValueText);
        }
    }

    private static bool IsMutableCollectionType(TypeSyntax typeSyntax)
    {
        var simpleName = typeSyntax switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified => GetRightmostName(qualified.Right),
            AliasQualifiedNameSyntax alias => GetRightmostName(alias.Name),
            NullableTypeSyntax nullable => IsMutableCollectionType(nullable.ElementType) ? "__mutable__" : string.Empty,
            _ => string.Empty
        };

        return simpleName == "__mutable__" || MutableCollectionTypeNames.Contains(simpleName);
    }

    private static string GetRightmostName(SimpleNameSyntax name) => name.Identifier.ValueText;

    private static Dictionary<string, StaticStateException> LoadExceptions(string root)
    {
        var path = Path.Combine(root, "UniversalToolchain", "Tests", "Architecture", "static-state-exceptions.json");
        var json = File.ReadAllText(path);
        var entries = JsonSerializer.Deserialize<List<StaticStateExceptionDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Static-state exception registry is null.");

        var result = new Dictionary<string, StaticStateException>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (!DateOnly.TryParse(entry.Expires, out var expires))
                throw new InvalidOperationException($"Static-state exception '{entry.Identity}' has invalid expiry '{entry.Expires}'.");

            var exception = new StaticStateException(entry.Identity, entry.Owner, entry.Reason, expires);
            if (!result.TryAdd(exception.Identity, exception))
                throw new InvalidOperationException($"Duplicate static-state exception '{exception.Identity}'.");
        }

        return result;
    }

    private static void ValidateException(
        StaticStateException exception,
        string identity,
        DateOnly today,
        ICollection<string> violations)
    {
        if (string.IsNullOrWhiteSpace(exception.Owner))
            violations.Add($"{identity}: exception owner is required");
        if (string.IsNullOrWhiteSpace(exception.Reason))
            violations.Add($"{identity}: exception reason is required");
        if (exception.Expires < today)
            violations.Add($"{identity}: exception expired on {exception.Expires:yyyy-MM-dd}");
    }

    private static bool IsProductionSource(string path)
    {
        var normalized = NormalizePath(path);
        return !normalized.Contains("/bin/", StringComparison.Ordinal)
               && !normalized.Contains("/obj/", StringComparison.Ordinal)
               && !normalized.Contains("/Tests/", StringComparison.Ordinal)
               && !normalized.EndsWith(".g.cs", StringComparison.Ordinal)
               && !normalized.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null)
        {
            var marker = Path.Combine(directory.FullName, "UniversalToolchain", "Tests", "Tests.csproj");
            if (File.Exists(marker))
                return directory.FullName;

            directory = directory.Parent;
        }

        Assert.Fail("Repository root was not found from the test directory.");
        return string.Empty;
    }

    private static string NormalizePath(string path) => path.Replace(Path.DirectorySeparatorChar, '/');

    private sealed record StaticFieldFinding(string RelativePath, string Owner, string Field)
    {
        public string Identity => $"{RelativePath}::{Owner}.{Field}";
    }

    private sealed record StaticStateException(string Identity, string Owner, string Reason, DateOnly Expires);

    private sealed record StaticStateExceptionDto(string Identity, string Owner, string Reason, string Expires);
}
