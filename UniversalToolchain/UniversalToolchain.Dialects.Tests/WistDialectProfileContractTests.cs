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
    public void PricingRestricted_DialectFile_UsesExpectedSurface()
    {
        var source = File.ReadAllText(GetDialectFilePath("pricing-restricted"));
        var usedModules = GetDirectiveItems(source, "use");

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("security restricted"));
            Assert.That(source, Does.Contain("backend cil,interpreter"));
            Assert.That(usedModules, Is.EquivalentTo(new[]
            {
                "Identifier",
                "NativeTypes",
                "Scopes",
                "Variables",
                "Whitespaces"
            }));
            Assert.That(usedModules, Does.Not.Contain("Arithmetic"));
            Assert.That(usedModules, Does.Not.Contain("BooleanConditions"));
            Assert.That(usedModules, Does.Not.Contain("ComparisonConditions"));
            Assert.That(usedModules, Does.Not.Contain("Conditions"));
            Assert.That(usedModules, Does.Not.Contain("Equality"));
            Assert.That(usedModules, Does.Not.Contain("ParametersSetter"));
            Assert.That(usedModules, Does.Not.Contain("SemicolonAsNewLine"));
        });
    }

    [Test]
    public void PricingRestricted_DialectFile_DoesNotExposeGeneralLanguageCapabilities()
    {
        var source = File.ReadAllText(GetDialectFilePath("pricing-restricted"));
        var usedModules = GetDirectiveItems(source, "use");

        Assert.Multiple(() =>
        {
            Assert.That(usedModules, Does.Not.Contain("Loops"));
            Assert.That(usedModules, Does.Not.Contain("Labels"));
            Assert.That(usedModules, Does.Not.Contain("Comments"));
            Assert.That(usedModules, Does.Not.Contain("CSharpInterop"));
            Assert.That(source, Does.Not.Contain("enable LocalVariablesOptimization"));
            Assert.That(source, Does.Not.Contain("capability unsafe-interop"));
        });
    }

    private static string GetDialectFilePath(string dialectName) =>
        Path.GetFullPath(Path.Combine(GetRepositoryRoot(), "Dialects", "examples", "wist", dialectName, "dialect.wistdialect"));

    private static string GetRepositoryFilePath(params string[] parts) =>
        Path.GetFullPath(Path.Combine([GetRepositoryRoot(), .. parts]));

    private static string GetRepositoryRoot() => TestContext.CurrentContext.TestDirectory;

    private static IReadOnlyList<string> GetDirectiveItems(string source, string directive)
    {
        var prefix = directive + " ";
        var line = source
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Single(x => x.StartsWith(prefix, StringComparison.Ordinal));

        return line[prefix.Length..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}