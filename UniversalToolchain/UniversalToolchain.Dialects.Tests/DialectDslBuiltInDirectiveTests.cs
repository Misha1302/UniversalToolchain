using CommonExceptions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDslBuiltInDirectiveTests
{
    [Test]
    public void UseDirective_ShouldParseValidateAndLowerIntoUseModuleList()
    {
        var slice = DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nuse Arithmetic,Variables\n");

        Assert.That(slice.UseModules, Is.EqualTo(new[] { "Arithmetic", "Variables" }));
    }

    [Test]
    public void UseDirective_ShouldRejectDuplicateIdentifiersWithinSingleDirective()
    {
        var ex = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nuse Arithmetic,Arithmetic\n"));

        DialectDslTestSupport.AssertParserExceptionContains(ex!, "duplicate identifiers");
    }

    [Test]
    public void ExcludeDirective_ShouldRejectUseExcludeConflictsAcrossDocument()
    {
        var ex = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nuse Arithmetic\nexclude Arithmetic\n"));

        DialectDslTestSupport.AssertParserExceptionContains(ex!, "Arithmetic", "use", "exclude");
    }

    [Test]
    public void RequiresBeforeAfterDirectives_ShouldLowerIntoOrderedRelations()
    {
        var slice = DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nrequires Core,Runtime\nbefore Parsing,Lowering\nafter Loading,Binding\n");

        Assert.That(slice.OrderDirectives.Select(x => (x.Kind, x.SourceModule, x.TargetModule)).ToArray(), Is.EqualTo(new[]
        {
            (DialectOrderDirectiveKind.Requires, "Core", "Runtime"),
            (DialectOrderDirectiveKind.Before, "Parsing", "Lowering"),
            (DialectOrderDirectiveKind.After, "Loading", "Binding")
        }));
    }

    [Test]
    public void BeforeDirective_ShouldRejectDuplicateTargetsAcrossMultipleDeclarations()
    {
        var ex = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nbefore Parsing\nbefore Parsing\n"));

        DialectDslTestSupport.AssertParserExceptionContains(ex!, "Duplicate before module is not allowed");
    }

    [Test]
    public void BackendDirective_ShouldParseMultipleBackends_AndRejectDuplicatesAcrossDocument()
    {
        var success = DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nbackend cil,interpreter\n");
        var duplicate = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nbackend cil\nbackend cil\n"));

        Assert.Multiple(() =>
        {
            Assert.That(success.BackendDirectives.Select(x => (x.Backend, x.Enabled)), Is.EqualTo(new[]
            {
                (DialectBackendTarget.Cil, true),
                (DialectBackendTarget.Interpreter, true)
            }));
            DialectDslTestSupport.AssertParserExceptionContains(duplicate!, "Duplicate backend identifier is not allowed");
        });
    }

    [Test]
    public void AllowAndForbidDirectives_ShouldTrackIntrinsicPolicy_AndRejectContradictions()
    {
        var success = DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nallow add_i32\nforbid sub_i32\n");
        var contradiction = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nallow add_i32\nforbid add_i32\n"));

        Assert.Multiple(() =>
        {
            Assert.That(success.IntrinsicDirectives.Select(x => (x.Name, x.Allowed)), Is.EqualTo(new[]
            {
                ("add_i32", true),
                ("sub_i32", false)
            }));
            DialectDslTestSupport.AssertParserExceptionContains(contradiction!, "cannot be both allowed and forbidden", "add_i32");
        });
    }

    [Test]
    public void EnableAndDisableDirectives_ShouldTrackOptimizerPolicy_AndRejectDuplicates()
    {
        var success = DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nenable Ssa\ndisable Inlining\n");
        var duplicate = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nenable Ssa\nenable Ssa\n"));

        Assert.Multiple(() =>
        {
            Assert.That(success.OptimizerDirectives.Select(x => (x.Name, x.Enabled)), Is.EqualTo(new[]
            {
                ("Ssa", true),
                ("Inlining", false)
            }));
            DialectDslTestSupport.AssertParserExceptionContains(duplicate!, "Duplicate enable optimizer directive is not allowed");
        });
    }

    [Test]
    public void SecurityDirective_ShouldBehaveAsSingleton_AndRejectUnsupportedProfiles()
    {
        var trusted = DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nsecurity trusted\n");
        var duplicate = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nsecurity trusted\nsecurity restricted\n"));
        var invalid = Assert.Throws<ArgumentException>(() => DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\nsecurity unknown\n"));

        Assert.Multiple(() =>
        {
            Assert.That(trusted.SecurityProfile, Is.EqualTo(DialectSecurityProfile.Trusted));
            DialectDslTestSupport.AssertParserExceptionContains(duplicate!, "Security directive can only be declared once");
            Assert.That(invalid!.Message, Does.Contain("Security profile 'unknown' is not supported"));
        });
    }

    [Test]
    public void CapabilityDirective_ShouldLowerIntoCapabilities_AndRejectDuplicatesAcrossDocument()
    {
        var success = DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\ncapability sandbox\ncapability unsafe-interop\n");
        var duplicate = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile("dialect Demo\ncapability sandbox\ncapability sandbox\n"));

        Assert.Multiple(() =>
        {
            Assert.That(success.CapabilityDirectives.Select(x => x.Name), Is.EqualTo(new[] { "sandbox", "unsafe-interop" }));
            DialectDslTestSupport.AssertParserExceptionContains(duplicate!, "Duplicate capability identifier is not allowed");
        });
    }

    [Test]
    public void BuiltInDocument_WithEveryDirective_ShouldProduceExpectedSemanticSlice()
    {
        var slice = DialectDslTestComposition.CreateCompiler().Compile(
            "dialect Demo\nuse Arithmetic,Variables\nexclude Legacy\nrequires Core,Runtime\nbefore Parsing,Lowering\nafter Loading,Binding\nbackend cil\nallow add_i32\nforbid sub_i32\nenable Ssa\ndisable Inlining\nsecurity restricted\ncapability sandbox\n");

        Assert.Multiple(() =>
        {
            Assert.That(slice.Name, Is.EqualTo("Demo"));
            Assert.That(slice.UseModules, Is.EqualTo(new[] { "Arithmetic", "Variables" }));
            Assert.That(slice.ExcludeModules, Is.EqualTo(new[] { "Legacy" }));
            Assert.That(slice.OrderDirectives.Select(x => (x.Kind, x.TargetModule)).ToArray(), Is.EqualTo(new[]
            {
                (DialectOrderDirectiveKind.Requires, "Runtime"),
                (DialectOrderDirectiveKind.Before, "Lowering"),
                (DialectOrderDirectiveKind.After, "Binding")
            }));
            Assert.That(slice.BackendDirectives.Select(x => x.Backend), Is.EqualTo(new[] { DialectBackendTarget.Cil }));
            Assert.That(slice.IntrinsicDirectives.Select(x => (x.Name, x.Allowed)), Is.EqualTo(new[] { ("add_i32", true), ("sub_i32", false) }));
            Assert.That(slice.OptimizerDirectives.Select(x => (x.Name, x.Enabled)), Is.EqualTo(new[] { ("Ssa", true), ("Inlining", false) }));
            Assert.That(slice.SecurityProfile, Is.EqualTo(DialectSecurityProfile.Restricted));
            Assert.That(slice.CapabilityDirectives.Select(x => x.Name), Is.EqualTo(new[] { "sandbox" }));
        });
    }
}