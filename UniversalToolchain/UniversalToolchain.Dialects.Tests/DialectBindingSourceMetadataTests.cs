using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Tests;

public class DialectBindingSourceMetadataTests
{
    [Test]
    public void SyntaxDialectBindingSource_ExposesVersion_FromDocument()
    {
        var source = new SyntaxDialectBindingSource(CreateSyntaxDocument(version: "1.2"));

        Assert.That(source.Version, Is.EqualTo("1.2"));
    }

    [Test]
    public void SyntaxDialectBindingSource_ExposesBaseDialectName_FromDocument()
    {
        var source = new SyntaxDialectBindingSource(CreateSyntaxDocument(baseDialectName: "base"));

        Assert.That(source.BaseDialectName, Is.EqualTo("base"));
    }

    [Test]
    public void CompiledDialectBindingSource_ExposesVersion_FromSlice()
    {
        var source = new CompiledDialectBindingSource(CreateCompiledSlice(version: "2.0"));

        Assert.That(source.Version, Is.EqualTo("2.0"));
    }

    [Test]
    public void CompiledDialectBindingSource_ExposesBaseDialectName_FromSlice()
    {
        var source = new CompiledDialectBindingSource(CreateCompiledSlice(baseDialectName: "base"));

        Assert.That(source.BaseDialectName, Is.EqualTo("base"));
    }

    private static DialectSyntaxDocument CreateSyntaxDocument(
        string? version = null,
        string? baseDialectName = null)
    {
        return new DialectSyntaxDocument(
            "dialect",
            version,
            [],
            [],
            [],
            [],
            [],
            [],
            SecurityProfile.Trusted,
            [],
            baseDialectName);
    }

    private static DialectDefinitionSlice CreateCompiledSlice(
        string? version = null,
        string? baseDialectName = null)
    {
        return new DialectDefinitionSlice(
            "dialect",
            [],
            [],
            [],
            [],
            [],
            [],
            DialectSecurityProfile.Trusted,
            [],
            version,
            baseDialectName);
    }
}
