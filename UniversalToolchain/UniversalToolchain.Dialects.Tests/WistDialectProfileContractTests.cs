namespace UniversalToolchain.Dialects.Tests;

public class WistDialectProfileContractTests
{
    [Test]
    public void FullDefault_DialectFile_ContainsAllUniversalUserFacingModules()
    {
        var source = File.ReadAllText(GetDialectFilePath("full-default"));

        Assert.That(source, Does.Contain("use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces"));
    }

    [Test]
    public void FullDefault_DialectFile_EnablesExpectedOptimizers()
    {
        var source = File.ReadAllText(GetDialectFilePath("full-default"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("enable BooleanOptimization"));
            Assert.That(source, Does.Contain("enable ComparisonIntrinsicOptimization"));
            Assert.That(source, Does.Contain("enable LocalVariablesOptimization"));
            Assert.That(source, Does.Contain("security trusted"));
            Assert.That(source, Does.Contain("capability unsafe-interop"));
        });
    }

    [Test]
    public void FullDefaultNative_DialectFile_UsesNativeTypes_AndDoesNotUseUniversalArithmeticModules()
    {
        var source = File.ReadAllText(GetDialectFilePath("full-default-native"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("NativeTypes"));
            Assert.That(source, Does.Not.Contain("use Arithmetic"));
            Assert.That(source, Does.Not.Contain(",Numbers,"));
            Assert.That(source, Does.Contain("security trusted"));
            Assert.That(source, Does.Contain("capability unsafe-interop"));
        });
    }

    [Test]
    public void InternalNativeCompositionPaths_UseCanonicalNativeTypesAlias()
    {
        var testBaseSource = File.ReadAllText(GetRepositoryFilePath("UniversalToolchain", "Tests", "TestBase.cs"));
        var exampleRunnerSource = File.ReadAllText(GetRepositoryFilePath("UniversalToolchain", "Example", "ExampleRunner.cs"));
        var cliProgramSource = File.ReadAllText(GetRepositoryFilePath("UniversalToolchain", "Wistc", "Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(testBaseSource, Does.Contain("Arithmetic,NativeTypes,Equality"), "Legacy test native profile must compose the canonical NativeTypes alias.");
            Assert.That(testBaseSource, Does.Not.Contain("Arithmetic,NativeMath,Equality"), "Legacy test native profile must not reintroduce the old NativeMath alias.");
            Assert.That(exampleRunnerSource, Does.Contain("Arithmetic,NativeTypes,Equality"), "ExampleRunner must compose the canonical NativeTypes alias.");
            Assert.That(exampleRunnerSource, Does.Not.Contain("Arithmetic,NativeMath,Equality"), "ExampleRunner must not reintroduce the old NativeMath alias.");
            Assert.That(cliProgramSource, Does.Contain("modules.Add(\"NativeTypes\")"), "CLI native path must add the canonical NativeTypes alias.");
            Assert.That(cliProgramSource, Does.Not.Contain("modules.Add(\"NativeMath\")"), "CLI native path must not add the old NativeMath alias.");
        });
    }

    [Test]
    public void RestrictedSandbox_DialectFile_IsInterpreterOnly_AndDisablesInteropStateAndControlFlow()
    {
        var source = File.ReadAllText(GetDialectFilePath("restricted-sandbox"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("backend interpreter"));
            Assert.That(source, Does.Not.Contain("backend cil"));
            Assert.That(source, Does.Contain("exclude CSharpInterop"));
            Assert.That(source, Does.Contain("Variables"));
            Assert.That(source, Does.Contain("Identifier"));
            Assert.That(source, Does.Contain("Loops"));
            Assert.That(source, Does.Contain("Labels"));
        });
    }

    [Test]
    public void RestrictedSandbox_Compose_ContainsRestrictedSecurityAndSandboxCapability()
    {
        var source = File.ReadAllText(GetDialectFilePath("restricted-sandbox"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("security restricted"));
            Assert.That(source, Does.Contain("capability sandbox"));
            Assert.That(source, Does.Not.Contain("capability unsafe-interop"));
        });
    }

    [Test]
    public void RepositoryReadme_ListsAllPublishedDialectExamples()
    {
        var source = File.ReadAllText(GetRepositoryReadmePath());

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("- `full-default`"));
            Assert.That(source, Does.Contain("- `full-default-native`"));
            Assert.That(source, Does.Contain("- `minimal-arithmetic`"));
            Assert.That(source, Does.Contain("- `restricted-sandbox`"));
        });
    }

    private static string GetDialectFilePath(string dialectName) =>
        Path.GetFullPath(Path.Combine(GetRepositoryRoot(), "Dialects", "examples", "wist", dialectName, "dialect.wistdialect"));

    private static string GetRepositoryReadmePath() =>
        GetRepositoryFilePath("readme.md");

    private static string GetRepositoryFilePath(params string[] parts) =>
        Path.GetFullPath(Path.Combine([GetRepositoryRoot(), .. parts]));

    private static string GetRepositoryRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
}
