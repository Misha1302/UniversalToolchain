using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class CompilerFactStateTests
{
    [Test]
    public void GetValidity_DistinguishesValidInvalidAndUnknownFacts()
    {
        var valid = new CompilerFactId("test.fact.valid");
        var invalid = new CompilerFactId("test.fact.invalid");
        var unknown = new CompilerFactId("test.fact.unknown");
        var state = new CompilerFactState(
            new HashSet<CompilerFactId> { valid },
            new HashSet<CompilerFactId> { invalid });

        Assert.Multiple(() =>
        {
            Assert.That(state.GetValidity(valid), Is.EqualTo(CompilerFactValidity.Valid));
            Assert.That(state.GetValidity(invalid), Is.EqualTo(CompilerFactValidity.Invalid));
            Assert.That(state.GetValidity(unknown), Is.EqualTo(CompilerFactValidity.Unknown));
        });
    }

    [Test]
    public void Constructor_RejectsContradictoryFactState()
    {
        var fact = new CompilerFactId("test.fact.contradictory");
        Assert.Throws<ArgumentException>(() => new CompilerFactState(
            new HashSet<CompilerFactId> { fact },
            new HashSet<CompilerFactId> { fact }));
    }
}
