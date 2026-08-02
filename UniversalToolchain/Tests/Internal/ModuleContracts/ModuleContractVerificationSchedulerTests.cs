using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class ModuleContractVerificationSchedulerTests
{
    [TestCase(ModuleContractVerificationPolicy.P0Structural)]
    [TestCase(ModuleContractVerificationPolicy.P1Invalidation)]
    public void NonSchedulingPolicies_ReturnNoSemanticInvocations(ModuleContractVerificationPolicy policy)
    {
        var scheduled = ModuleContractVerificationScheduler.Schedule(
            policy,
            Routes(),
            Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified));

        Assert.That(scheduled, Is.Empty);
    }

    [Test]
    public void Selective_MergesAndSchedulesOnlyRequestedRoute()
    {
        var requests = new[]
        {
            new ReverificationRequest(
                KnownCoreVerifierRules.AirContract,
                [KnownCoreCompilerFacts.AirVerified, KnownCoreCompilerFacts.AirStackBalanced]),
            new ReverificationRequest(
                KnownCoreVerifierRules.AirContract,
                [KnownCoreCompilerFacts.AirVerified])
        };

        var scheduled = ModuleContractVerificationScheduler.Schedule(
            ModuleContractVerificationPolicy.P2Selective,
            Routes(),
            requests);

        Assert.Multiple(() =>
        {
            Assert.That(scheduled, Has.Count.EqualTo(1));
            Assert.That(scheduled[0].RuleId, Is.EqualTo(KnownCoreVerifierRules.AirContract));
            Assert.That(scheduled[0].CanonicalOwner, Is.EqualTo("core.air"));
            Assert.That(scheduled[0].IsObligationDriven, Is.True);
            Assert.That(
                scheduled[0].InvalidatedFacts,
                Is.EqualTo(new[]
                {
                    KnownCoreCompilerFacts.AirStackBalanced,
                    KnownCoreCompilerFacts.AirVerified
                }.OrderBy(static fact => fact.Value, StringComparer.Ordinal)));
        });
    }

    [Test]
    public void Always_SchedulesAllRoutesInDeterministicOrder()
    {
        var scheduled = ModuleContractVerificationScheduler.Schedule(
            ModuleContractVerificationPolicy.P3Always,
            Routes().Reverse().ToArray(),
            Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified));

        Assert.Multiple(() =>
        {
            Assert.That(
                scheduled.Select(static invocation => invocation.RuleId),
                Is.EqualTo(new[]
                {
                    KnownCoreVerifierRules.AirContract,
                    KnownCoreVerifierRules.BytecodeContract
                }.OrderBy(static rule => rule.Value, StringComparer.Ordinal)));
            Assert.That(
                scheduled.Single(static invocation => invocation.RuleId == KnownCoreVerifierRules.AirContract)
                    .IsObligationDriven,
                Is.True);
            Assert.That(
                scheduled.Single(static invocation => invocation.RuleId == KnownCoreVerifierRules.BytecodeContract)
                    .IsObligationDriven,
                Is.False);
        });
    }

    [Test]
    public void UnknownPolicy_FailsClosed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ModuleContractVerificationScheduler.Schedule(
            (ModuleContractVerificationPolicy)int.MaxValue,
            Routes(),
            []));
    }

    [Test]
    public void UnknownObligation_FailsClosed()
    {
        var unknown = new VerifierRuleId("test.verifier.unknown");

        Assert.Throws<InvalidOperationException>(() => ModuleContractVerificationScheduler.Schedule(
            ModuleContractVerificationPolicy.P2Selective,
            Routes(),
            Requests(unknown, new CompilerFactId("test.fact.unknown"))));
    }

    [Test]
    public void ConflictingCanonicalOwners_FailClosed()
    {
        var routes = new[]
        {
            new ModuleContractVerifierRoute(KnownCoreVerifierRules.AirContract, "owner.a"),
            new ModuleContractVerifierRoute(KnownCoreVerifierRules.AirContract, "owner.b")
        };

        Assert.Throws<InvalidOperationException>(() => ModuleContractVerificationScheduler.Schedule(
            ModuleContractVerificationPolicy.P3Always,
            routes,
            []));
    }

    [Test]
    public void SelectiveRoutes_AreSubsetOfAlwaysRoutes()
    {
        var routes = Routes();
        var requests = Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified);
        var selective = ModuleContractVerificationScheduler.Schedule(
                ModuleContractVerificationPolicy.P2Selective,
                routes,
                requests)
            .Select(static invocation => invocation.RuleId)
            .ToHashSet();
        var always = ModuleContractVerificationScheduler.Schedule(
                ModuleContractVerificationPolicy.P3Always,
                routes,
                requests)
            .Select(static invocation => invocation.RuleId)
            .ToHashSet();

        Assert.That(selective.IsSubsetOf(always), Is.True);
    }

    private static IReadOnlyList<ModuleContractVerifierRoute> Routes() =>
    [
        new(KnownCoreVerifierRules.BytecodeContract, "core.bytecode"),
        new(KnownCoreVerifierRules.AirContract, "core.air")
    ];

    private static IReadOnlyList<ReverificationRequest> Requests(VerifierRuleId rule, CompilerFactId fact) =>
        [new ReverificationRequest(rule, [fact])];
}
