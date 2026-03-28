namespace UniversalToolchain.Dialects.Tests;

public class WistPackageCouplingTests
{
    [Test]
    public void WistProject_DoesNotReferenceFeatureModules_InMinimalArchitecture()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "..",
            "UniversalToolchain.Dialects.Wist",
            "UniversalToolchain.Dialects.Wist.csproj"));

        var projectText = File.ReadAllText(projectPath);

        var forbiddenReferences = new[]
        {
            "ArithmeticModule",
            "CommentsModule",
            "ConditionsModule",
            "CSharpInteropModule",
            "EqualityModule",
            "IdentifierModule",
            "InternalPreprocessorLexemesModule",
            "LabelsModule",
            "LocalVariablesOptimizerModule",
            "LoopsModule",
            "NativeMathModule",
            "NumbersModule",
            "ParametersSetterModule",
            "ScopesModule",
            "SemicolonAsNewLineModule",
            "VariablesModule",
            "WhitespacesModule"
        };

        foreach (var referenceName in forbiddenReferences)
            Assert.That(projectText, Does.Not.Contain(referenceName), $"Wist host package must not compile-time reference '{referenceName}'.");
    }
}
