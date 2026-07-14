namespace Tests.Architecture;

[TestFixture]
public sealed class HardcodeBoundaryGuardrailTests
{
    [Test]
    public void BasicCore_ShouldNotKnowConcreteVariablesAstContracts()
    {
        var root = FindRepositoryRoot();
        var files = ProductionFiles(root, "UniversalToolchain/BasicCore");
        var forbiddenPatterns = new[]
        {
            "CreateOrGet(\"Variable\")",
            "\"VariableDefinition\"",
            "\"VariableDefinitionWithType\"",
            "VariablesModule"
        };

        AssertNoPatterns(root, files, forbiddenPatterns,
            "BasicCore must keep binding generic; concrete variable syntax belongs to VariablesModule binding rules.");
    }

    [Test]
    public void WistEngine_ShouldNotReferenceConcreteCilArtifacts()
    {
        var root = FindRepositoryRoot();
        var wistEngine = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Wist", "WistEngine.cs");
        var forbiddenPatterns = new[]
        {
            "BasicCilCompiler",
            "CilCompilationOutput",
            "GetBackendSpecificArtifactCompiler<",
            "DynamicMethod"
        };

        AssertNoPatterns(root, [wistEngine], forbiddenPatterns,
            "WistEngine should stay a public facade over the selected runtime plan; explicit CIL fast paths belong to named adapters.");
    }

    [Test]
    public void EqualityModule_ShouldUseTypedWriteTargetSemanticContract()
    {
        var root = FindRepositoryRoot();
        var files = ProductionFiles(root, "UniversalToolchain/EqualityModule");
        var forbiddenPatterns = new[]
        {
            "ExpectingWriteTypeInference",
            "AddTag(\""
        };

        AssertNoPatterns(root, files, forbiddenPatterns,
            "Assignment/write-target semantics must use AssignmentSemanticContractIds.WriteTarget, not cross-module raw tags.");
    }

    [Test]
    public void VariablesVisitor_ShouldNotParseRawPreprocessorDirectiveSyntax()
    {
        var root = FindRepositoryRoot();
        var variablesVisitor = Path.Combine(root, "UniversalToolchain", "VariablesModule", "VariablesVisitor.cs");
        var forbiddenPatterns = new[]
        {
            "Text[3..^1]",
            ".Split()",
            "[\"define\"",
            "\"define\", _, \"as\""
        };

        AssertNoPatterns(root, [variablesVisitor], forbiddenPatterns,
            "Preprocessor directive syntax belongs to InternalPreprocessorLexemesModule; VariablesVisitor must consume structured directive contracts.");
    }

    [Test]
    public void FrameworkRuntimeLayers_ShouldNotUseWistThrower()
    {
        var root = FindRepositoryRoot();
        var guardedDirectories = new[]
        {
            "UniversalToolchain/BasicCore",
            "UniversalToolchain/BasicLexer",
            "UniversalToolchain/BasicInterpreter",
            "UniversalToolchain/UniversalToolchain.Dialects.Frontend",
            "UniversalToolchain/InternalPreprocessorLexemesModule",
            "UniversalToolchain/ScopesModule"
        };

        var files = guardedDirectories.SelectMany(directory => ProductionFiles(root, directory)).ToList();
        AssertNoPatterns(root, files, ["WistThrower"],
            "Generic framework/runtime layers must use ToolchainThrower; the removed WistThrower compatibility alias must not return.");
    }

    [Test]
    public void PreparedExecutionBuilder_ShouldDecodeIntrinsicsBeforeRuntimePlanning()
    {
        var root = FindRepositoryRoot();
        var builder = Path.Combine(root, "UniversalToolchain", "BasicCore", "Core", "PreparedExecutionBuilder.cs");

        AssertNoPatterns(root, [builder], ["\"call C#\""],
            "Runtime provider extraction must operate on BuiltinIntrinsicSymbols.Core.CallCSharp after legacy decoding, not raw intrinsic display strings.");
    }


    [Test]
    public void ReusableModuleDescriptors_ShouldNotDependOnWistContractsPackage()
    {
        var root = FindRepositoryRoot();
        var guardedProjects = new[]
        {
            "UniversalToolchain/IdentifierModule/IdentifierModule.csproj",
            "UniversalToolchain/ScopesModule/ScopesModule.csproj",
            "UniversalToolchain/VariablesModule/VariablesModule.csproj"
        };
        var guardedSources = new[]
        {
            "UniversalToolchain/IdentifierModule",
            "UniversalToolchain/ScopesModule",
            "UniversalToolchain/VariablesModule"
        }.SelectMany(directory => ProductionFiles(root, directory)).ToList();

        AssertNoPatterns(root, guardedProjects.Select(path => Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))),
            ["UniversalToolchain.Wist.Contracts"],
            "Reusable language modules must own their compiler facts; Wist.Contracts is a Wist compatibility edge, not a dependency of identifiers/scopes/variables.");
        AssertNoPatterns(root, guardedSources, ["UniversalToolchain.Wist.Contracts", "WistIdentifierFacts", "WistScopesFacts"],
            "Reusable module descriptors must depend on module-owned facts, not Wist-owned aliases.");
    }

    [Test]
    public void ProductionOptimizers_ShouldNotCompareLegacyCSharpCallDisplayName()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            "UniversalToolchain/NativeMathModule",
            "UniversalToolchain/ConditionsModule/Optimizers",
            "UniversalToolchain/BytecodeDynamicMethodsCompiler/Compilers/CilExecutionRequirementAnalyzer.cs"
        }.SelectMany(path =>
            Directory.Exists(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)))
                ? ProductionFiles(root, path)
                : new[] { Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)) })
            .ToList();

        AssertNoPatterns(root, files, ["\"call C#\""],
            "Optimizers and backend planning must read CallCSharp through the typed intrinsic model; raw display strings belong only to legacy decoder/registry compatibility surfaces.");
    }

    [Test]
    public void PreparedExecutionBuilder_ShouldUseCompositionOwnedRuntimeProviderPolicy()
    {
        var root = FindRepositoryRoot();
        var builder = Path.Combine(root, "UniversalToolchain", "BasicCore", "Core", "PreparedExecutionBuilder.cs");
        var content = File.ReadAllText(builder);

        Assert.That(content, Does.Contain("IRuntimeProviderPolicyComponent"));
        Assert.That(content, Does.Not.Contain("ExtractAllowedRuntimeProviderTypes"),
            "Runtime provider allowlist must come from selected backend/runtime composition, not from scanning AIR after optimization.");
    }

    private static IReadOnlyList<string> ProductionFiles(string root, string relativeDirectory)
    {
        var directory = Path.Combine(root, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !Normalize(path).Contains("/bin/", StringComparison.Ordinal))
            .Where(static path => !Normalize(path).Contains("/obj/", StringComparison.Ordinal))
            .Where(static path => !Normalize(path).Contains("/Tests/", StringComparison.Ordinal))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToList();
    }

    private static void AssertNoPatterns(string root, IEnumerable<string> files, IReadOnlyList<string> forbiddenPatterns, string message)
    {
        var violations = new List<string>();
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            foreach (var pattern in forbiddenPatterns)
            {
                if (content.Contains(pattern, StringComparison.Ordinal))
                    violations.Add($"{Normalize(Path.GetRelativePath(root, file))}: contains forbidden pattern {pattern}");
            }
        }

        Assert.That(violations, Is.Empty, message);
    }

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

    private static string Normalize(string path) => path.Replace(Path.DirectorySeparatorChar, '/');
}
