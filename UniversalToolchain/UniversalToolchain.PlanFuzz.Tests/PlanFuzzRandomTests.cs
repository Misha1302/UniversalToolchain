using UniversalToolchain.PlanFuzz;

namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class PlanFuzzRandomTests
{
    [Test]
    public void NextUInt64MatchesVersionOneGoldenVector()
    {
        var random = new PlanFuzzRandom(1);
        var actual = Enumerable.Range(0, 8).Select(_ => random.NextUInt64()).ToArray();
        ulong[] expected =
        [
            0xb3f2af6d0fc710c5UL,
            0x853b559647364ceaUL,
            0x92f89756082a4514UL,
            0x642e1c7bc266a3a7UL,
            0xb27a48e29a233673UL,
            0x24c123126ffda722UL,
            0x123004ef8df510e6UL,
            0x61954dcc47b1e89dUL
        ];
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ForkIsDomainSeparatedAndIndependentOfParentConsumption()
    {
        var untouchedParent = new PlanFuzzRandom(20260724);
        var consumedParent = new PlanFuzzRandom(20260724);
        _ = consumedParent.NextUInt64();
        _ = consumedParent.NextUInt64();

        var first = untouchedParent.Fork("program");
        var second = consumedParent.Fork("program");
        var otherDomain = untouchedParent.Fork("plan");

        Assert.That(first.NextUInt64(), Is.EqualTo(second.NextUInt64()));
        Assert.That(first.NextUInt64(), Is.Not.EqualTo(otherDomain.NextUInt64()));
    }
}
