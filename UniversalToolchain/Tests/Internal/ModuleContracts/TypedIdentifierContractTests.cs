using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class TypedIdentifierContractTests
{
    [Test]
    public void ModuleId_WhenValuesMatch_ComparesOrdinally()
    {
        var first = new ModuleId("core.module");
        var second = new ModuleId("core.module");
        var differentCase = new ModuleId("CORE.MODULE");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Is.Not.EqualTo(differentCase));
        Assert.That(first.ToString(), Is.EqualTo("core.module"));
    }

    [TestCase("")]
    [TestCase(" ")]
    public void ModuleId_WhenValueIsEmpty_RejectsValue(string value)
    {
        Assert.That(() => new ModuleId(value), Throws.ArgumentException);
    }

    [Test]
    public void TypedIds_WhenValuesMatchOnlyWithinType_DoNotCollapseAcrossContractKinds()
    {
        var astNodeKind = new AstNodeKind("core.shared");
        var bytecodeTagId = new BytecodeTagId("core.shared");

        Assert.That(astNodeKind.Value, Is.EqualTo(bytecodeTagId.Value));
        Assert.That(astNodeKind, Is.Not.EqualTo(new AstNodeKind("core.other")));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("fact-without-namespace")]
    [TestCase(".fact")]
    [TestCase("fact.")]
    [TestCase("fact..name")]
    public void CompilerFactId_WhenValueIsNotNamespaced_RejectsValue(string value)
    {
        Assert.That(() => new CompilerFactId(value), Throws.ArgumentException);
    }

    [TestCase("")]
    [TestCase("effect-without-namespace")]
    [TestCase("effect name.invalid")]
    public void CompilerEffectId_WhenValueIsNotNamespaced_RejectsValue(string value)
    {
        Assert.That(() => new CompilerEffectId(value), Throws.ArgumentException);
    }
}
