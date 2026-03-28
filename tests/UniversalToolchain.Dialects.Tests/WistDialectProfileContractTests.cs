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
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "samples", "dialects", "wist", dialectName, "dialect.wistdialect"));

    private static string GetRepositoryReadmePath() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "readme.md"));
}