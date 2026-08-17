namespace Tests.Architecture;

[SetUpFixture]
public sealed class WistSinglePathGateSetup
{
    [OneTimeSetUp]
    public void AssertCanonicalAndMutations()
    {
        var clean = ReadGateInput();
        var cleanViolations = Analyze(clean);
        Assert.That(cleanViolations, Is.Empty, string.Join(Environment.NewLine, cleanViolations));

        var mutations = new (string Name, GateInput Input)[]
        {
            (
                "semantic artifact carries AST again",
                clean with
                {
                    ArtifactStagesSource = clean.ArtifactStagesSource.Replace(
                        "internal sealed class WistSemanticArtifact(CompilationInput input, WistSemanticProgram program)",
                        "internal sealed class WistSemanticArtifact(CompilationInput input, AstNode program)",
                        StringComparison.Ordinal)
                }),
            (
                "old AST translator fallback returns",
                clean with
                {
                    RuntimeSource = clean.RuntimeSource + Environment.NewLine +
                                    "// mutation: new BasicAstToBytecodeTranslatorImpl();"
                }),
            (
                "old translator project dependency returns",
                clean with
                {
                    ProjectSource = clean.ProjectSource + Environment.NewLine +
                                    "<ProjectReference Include=\"..\\BasicCodeTranslator\\BasicCodeTranslator.csproj\" />"
                }),
            (
                "second semantic lowerer owner is activated",
                clean with
                {
                    RuntimeSource = clean.RuntimeSource + Environment.NewLine +
                                    "// mutation: new WistSemanticBytecodeLowerer(plan, catalog, resolver, types);"
                }),
            (
                "semantic model stores parser node",
                clean with
                {
                    SemanticModelSource = clean.SemanticModelSource + Environment.NewLine +
                                          "// mutation: private AstNode _syntax;"
                })
        };

        foreach (var mutation in mutations)
        {
            Assert.That(
                Analyze(mutation.Input),
                Is.Not.Empty,
                $"Single-path architecture gate failed to kill mutation: {mutation.Name}");
        }
    }

    private static IReadOnlyList<string> Analyze(GateInput input)
    {
        var violations = new List<string>();

        var semanticArtifact = SliceBetween(
            input.ArtifactStagesSource,
            "internal sealed class WistSemanticArtifact",
            "internal sealed class WistBytecodeArtifact");
        if (semanticArtifact == null)
        {
            violations.Add("WistSemanticArtifact declaration is missing or cannot be isolated.");
        }
        else
        {
            if (!semanticArtifact.Contains("WistSemanticProgram program", StringComparison.Ordinal))
                violations.Add("WistSemanticArtifact no longer owns WistSemanticProgram as its semantic payload.");
            if (semanticArtifact.Contains("AstNode", StringComparison.Ordinal))
                violations.Add("WistSemanticArtifact carries AstNode across the semantic boundary.");
        }

        var bytecodeTransformer = SliceBetween(
            input.ArtifactStagesSource,
            "internal sealed class WistDirectBytecodeTransformer",
            "internal static class WistSyntaxPhaseExecution");
        if (bytecodeTransformer == null)
        {
            violations.Add("WistDirectBytecodeTransformer declaration is missing or cannot be isolated.");
        }
        else
        {
            if (!bytecodeTransformer.Contains("_lowerer.Lower(source.Program)", StringComparison.Ordinal))
                violations.Add("Canonical bytecode transformer no longer lowers the semantic program directly.");
            foreach (var marker in ForbiddenAstLoweringMarkers)
            {
                if (bytecodeTransformer.Contains(marker, StringComparison.Ordinal))
                    violations.Add($"Canonical bytecode transformer contains forbidden AST-lowering marker '{marker}'.");
            }
        }

        foreach (var (path, source) in input.LowererSources)
        {
            foreach (var marker in ForbiddenAstLoweringMarkers)
            {
                if (source.Contains(marker, StringComparison.Ordinal))
                    violations.Add($"Native semantic lowerer contains forbidden marker '{marker}': {path}");
            }
        }

        foreach (var marker in new[] { "AstNode", "BoundAstNode", "LexemeValue" })
        {
            if (input.SemanticModelSource.Contains(marker, StringComparison.Ordinal))
                violations.Add($"Data-only semantic model contains syntax/parser marker '{marker}'.");
        }

        foreach (var marker in new[]
                 {
                     "BasicAstToBytecodeTranslatorImpl",
                     "IAstToBytecodeTranslator",
                     "semanticToAst",
                     "SemanticToAst",
                     "ToAst("
                 })
        {
            if (input.RuntimeSource.Contains(marker, StringComparison.Ordinal))
                violations.Add($"Canonical Wist runtime contains projection/fallback marker '{marker}'.");
        }

        if (input.ProjectSource.Contains("BasicCodeTranslator", StringComparison.Ordinal))
            violations.Add("Wist LanguagePack still depends on the retired AST-to-bytecode translator project.");

        var lowererConstructionCount = CountOccurrences(
            input.RuntimeSource,
            "new WistSemanticBytecodeLowerer(");
        if (lowererConstructionCount != 1)
        {
            violations.Add(
                $"Canonical runtime must activate exactly one WistSemanticBytecodeLowerer owner; found {lowererConstructionCount}.");
        }

        return violations;
    }

    private static GateInput ReadGateInput()
    {
        var root = FindRepositoryRoot();
        var languagePack = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Wist.LanguagePack");
        var lowererSources = Directory.EnumerateFiles(
                languagePack,
                "WistSemanticBytecodeLowerer*.cs",
                SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(path => new SourceFile(
                NormalizePath(Path.GetRelativePath(root, path)),
                File.ReadAllText(path)))
            .ToArray();

        return new GateInput(
            File.ReadAllText(Path.Combine(languagePack, "WistDirectArtifactStages.cs")),
            File.ReadAllText(Path.Combine(languagePack, "WistDirectRuntimeComponents.cs")),
            File.ReadAllText(Path.Combine(languagePack, "WistSemanticModel.cs")),
            File.ReadAllText(Path.Combine(languagePack, "UniversalToolchain.Wist.LanguagePack.csproj")),
            lowererSources);
    }

    private static string? SliceBetween(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0)
            return null;
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        if (end < 0)
            return null;
        return source[start..end];
    }

    private static int CountOccurrences(string source, string marker)
    {
        var count = 0;
        var offset = 0;
        while (true)
        {
            var index = source.IndexOf(marker, offset, StringComparison.Ordinal);
            if (index < 0)
                return count;
            count++;
            offset = index + marker.Length;
        }
    }

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

    private static readonly string[] ForbiddenAstLoweringMarkers =
    [
        "AstNode",
        "BoundAstNode",
        "IAstVisitor",
        "IAstToBytecodeTranslator",
        "BasicAstToBytecodeTranslatorImpl",
        "LexemeValue"
    ];

    private sealed record SourceFile(string Path, string Source);

    private sealed record GateInput(
        string ArtifactStagesSource,
        string RuntimeSource,
        string SemanticModelSource,
        string ProjectSource,
        IReadOnlyList<SourceFile> LowererSources);
}
