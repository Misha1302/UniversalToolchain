using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaRouteReportInvariantTests
{
    [Test]
    public void Constructor_WhenRouteBothSucceedsAndFallsBack_RejectsState()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Create(
                SsaRoutePolicy.Prefer,
                usedSsa: true,
                fellBackToInput: true));

        Assert.That(exception!.Message, Does.Contain("both succeed and fall back"));
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void Constructor_WhenPolicyIsOffButRouteWasAttempted_RejectsState(
        bool usedSsa,
        bool fellBackToInput)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Create(
                SsaRoutePolicy.Off,
                usedSsa,
                fellBackToInput));

        Assert.That(exception!.Message, Does.Contain("disabled SSA route"));
    }

    [TestCase(SsaRoutePolicy.Require)]
    [TestCase(SsaRoutePolicy.Debug)]
    public void Constructor_WhenNonPreferPolicyFallsBack_RejectsState(
        SsaRoutePolicy policy)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Create(
                policy,
                usedSsa: false,
                fellBackToInput: true));

        Assert.That(exception!.Message, Does.Contain("Only the Prefer SSA policy"));
    }

    [Test]
    public void Constructor_WhenFallbackChangesInstructionCount_RejectsState()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Create(
                SsaRoutePolicy.Prefer,
                usedSsa: false,
                fellBackToInput: true,
                inputCount: 5,
                outputCount: 4));

        Assert.That(exception!.Message, Does.Contain("preserve the input AIR instruction count"));
    }

    [Test]
    public void Constructor_AcceptsCanonicalRouteOutcomeStates()
    {
        var reports = new[]
        {
            Create(SsaRoutePolicy.Off, usedSsa: false, fellBackToInput: false),
            Create(SsaRoutePolicy.Prefer, usedSsa: false, fellBackToInput: true),
            Create(SsaRoutePolicy.Require, usedSsa: true, fellBackToInput: false, outputCount: 2),
            Create(SsaRoutePolicy.Debug, usedSsa: false, fellBackToInput: false)
        };

        Assert.That(reports, Has.All.Not.Null);
    }

    private static SsaRouteReport Create(
        SsaRoutePolicy policy,
        bool usedSsa,
        bool fellBackToInput,
        int inputCount = 3,
        int? outputCount = null) =>
        new(
            policy,
            "test-profile",
            usedSsa,
            fellBackToInput,
            inputCount,
            outputCount ?? inputCount);
}
