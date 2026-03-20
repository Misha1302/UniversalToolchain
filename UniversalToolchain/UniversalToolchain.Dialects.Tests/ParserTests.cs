namespace UniversalToolchain.Dialects.Tests;

public class ParserTests
{
    [Test]
    public void Parse_MinimalValidDialect_Succeeds()
    {
        var parser = new DialectDefinitionParser();

        var result = parser.Parse("dialect Core\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(result.Document!.Name, Is.EqualTo("Core"));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Parse_CompleteValidDialect_Succeeds()
    {
        var parser = new DialectDefinitionParser();
        var source = """
                     dialect Strict version "1.0"
                     use Arithmetic
                     exclude CSharpInterop
                     requires Variables -> Scopes
                     before Conditions -> Labels
                     after Loops -> Labels
                     backend interpreter enable
                     backend cil disable
                     allow intrinsic "add_i32" for any
                     forbid intrinsic "unsafe_reflect" for cil
                     enable optimizer ConstFold for any
                     disable optimizer AggressiveInline for interpreter
                     security restricted
                     capability supports-floats = true
                     capability safe-interop = false
                     """;

        var result = parser.Parse(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(result.Document!.Version, Is.EqualTo("1.0"));
            Assert.That(result.Document.UseModules, Has.Count.EqualTo(1));
            Assert.That(result.Document.ExcludeModules, Has.Count.EqualTo(1));
            Assert.That(result.Document.OrderRules, Has.Count.EqualTo(3));
            Assert.That(result.Document.BackendDirectives, Has.Count.EqualTo(2));
            Assert.That(result.Document.IntrinsicDirectives, Has.Count.EqualTo(2));
            Assert.That(result.Document.OptimizerDirectives, Has.Count.EqualTo(2));
            Assert.That(result.Document.SecurityProfile, Is.EqualTo(SecurityProfile.Restricted));
            Assert.That(result.Document.Capabilities["supports-floats"], Is.True);
        });
    }

    [Test]
    public void Parse_MalformedArrow_AddsDiagnostic()
    {
        var parser = new DialectDefinitionParser();

        var result = parser.Parse("dialect A\nrequires A - B\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(x => x.Code == "P203"), Is.True);
        });
    }

    [Test]
    public void Parse_UnterminatedString_AddsDiagnostic()
    {
        var parser = new DialectDefinitionParser();

        var result = parser.Parse("dialect A\nallow intrinsic \"abc for any\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(x => x.Code == "P001"), Is.True);
        });
    }

    [Test]
    public void Parse_OpenEndedBackendIdentifier_IsAccepted()
    {
        var parser = new DialectDefinitionParser();

        var result = parser.Parse("dialect A\nbackend wasm enable\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Document!.BackendDirectives.Select(x => x.Backend.Value), Is.EqualTo(new[] { "wasm" }));
        });
    }

    [Test]
    public void Parse_DuplicateConflictingBackend_AddsDiagnostic()
    {
        var parser = new DialectDefinitionParser();

        var result = parser.Parse("dialect A\nbackend interpreter enable\nbackend interpreter disable\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(x => x.Code == "P103"), Is.True);
        });
    }

    [Test]
    public void Parse_DuplicateCapability_AddsDiagnostic()
    {
        var parser = new DialectDefinitionParser();

        var result = parser.Parse("dialect A\ncapability safe = true\ncapability safe = false\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(x => x.Code == "P106"), Is.True);
        });
    }

    [Test]
    public void Parse_SameInput_IsStable()
    {
        var parser = new DialectDefinitionParser();
        var source = "dialect A\nuse Arithmetic\nbackend cil enable\n";

        var first = parser.Parse(source);
        var second = parser.Parse(source);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.True);
            Assert.That(first.Document!.Name, Is.EqualTo(second.Document!.Name));
            Assert.That(first.Document.UseModules.SequenceEqual(second.Document.UseModules), Is.True);
            Assert.That(first.Diagnostics.Count, Is.EqualTo(second.Diagnostics.Count));
        });
    }

    [Test]
    public void Parse_ReadableDiagnostics_ContainLocationAndReason()
    {
        var parser = new DialectDefinitionParser();

        var result = parser.Parse("dialect\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(result.Diagnostics[0].Message, Does.Contain("line"));
            Assert.That(result.Diagnostics[0].Message, Does.Contain("Expected"));
        });
    }
}