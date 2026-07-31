using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.ContractExperiments;

internal static class VerificationPolicySchedulerTests
{
    public static void Run()
    {
        StructuralAndInvalidationDoNotScheduleSemanticVerification();
        SelectiveSchedulesOnlyRequestedCanonicalRoutes();
        AlwaysSchedulesEveryAvailableRouteDeterministically();
        UnknownObligationFailsClosed();
        ConflictingCanonicalOwnersFailClosed();
        SchedulingDoesNotMutateInputs();
        SelectiveIsSubsetOfAlwaysForSameBoundary();
    }

    private static void StructuralAndInvalidationDoNotScheduleSemanticVerification()
    {
        var routes = Routes();
        var requests = Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified);
        AssertCount(0, VerificationPolicyScheduler.Schedule(ExperimentPolicy.P0_STRUCTURAL, routes, requests));
        AssertCount(0, VerificationPolicyScheduler.Schedule(ExperimentPolicy.P1_INVALIDATION, routes, requests));
    }

    private static void SelectiveSchedulesOnlyRequestedCanonicalRoutes()
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
        var scheduled = VerificationPolicyScheduler.Schedule(
            ExperimentPolicy.P2_SELECTIVE,
            Routes(),
            requests);

        AssertCount(1, scheduled);
        AssertEqual(KnownCoreVerifierRules.AirContract, scheduled[0].RuleId, "selective rule");
        AssertEqual("core.air", scheduled[0].CanonicalOwner, "selective owner");
        AssertTrue(scheduled[0].IsObligationDriven, "selective invocation must be obligation-driven");
        AssertSequenceEqual(
            new[] { KnownCoreCompilerFacts.AirStackBalanced, KnownCoreCompilerFacts.AirVerified }
                .OrderBy(static fact => fact.Value, StringComparer.Ordinal)
                .ToArray(),
            scheduled[0].InvalidatedFacts,
            "selective facts");
    }

    private static void AlwaysSchedulesEveryAvailableRouteDeterministically()
    {
        var scheduled = VerificationPolicyScheduler.Schedule(
            ExperimentPolicy.P3_ALWAYS,
            Routes().Reverse().ToArray(),
            Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified));

        AssertCount(2, scheduled);
        AssertSequenceEqual(
            new[] { KnownCoreVerifierRules.AirContract, KnownCoreVerifierRules.BytecodeContract }
                .OrderBy(static rule => rule.Value, StringComparer.Ordinal)
                .ToArray(),
            scheduled.Select(static invocation => invocation.RuleId).ToArray(),
            "always deterministic order");
        AssertTrue(scheduled.Single(static item => item.RuleId == KnownCoreVerifierRules.AirContract).IsObligationDriven,
            "requested always route must retain obligation identity");
        AssertTrue(!scheduled.Single(static item => item.RuleId == KnownCoreVerifierRules.BytecodeContract).IsObligationDriven,
            "unrequested always route must be marked unconditional");
    }

    private static void UnknownObligationFailsClosed()
    {
        var unknown = new VerifierRuleId("experiment.unknown.verifier");
        AssertThrows<InvalidOperationException>(() => VerificationPolicyScheduler.Schedule(
            ExperimentPolicy.P2_SELECTIVE,
            Routes(),
            Requests(unknown, new CompilerFactId("experiment.unknown.fact"))));
        AssertThrows<InvalidOperationException>(() => VerificationPolicyScheduler.Schedule(
            ExperimentPolicy.P3_ALWAYS,
            Routes(),
            Requests(unknown, new CompilerFactId("experiment.unknown.fact"))));
    }

    private static void ConflictingCanonicalOwnersFailClosed()
    {
        var routes = new[]
        {
            new VerifierRouteDescriptor(KnownCoreVerifierRules.AirContract, "owner.a"),
            new VerifierRouteDescriptor(KnownCoreVerifierRules.AirContract, "owner.b")
        };
        AssertThrows<InvalidOperationException>(() => VerificationPolicyScheduler.Schedule(
            ExperimentPolicy.P2_SELECTIVE,
            routes,
            Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified)));
        AssertThrows<InvalidOperationException>(() => VerificationPolicyScheduler.Schedule(
            ExperimentPolicy.P3_ALWAYS,
            routes,
            []));
    }

    private static void SchedulingDoesNotMutateInputs()
    {
        var routes = Routes().ToList();
        var requests = Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified).ToList();
        var routeSnapshot = routes.ToArray();
        var requestSnapshot = requests.ToArray();

        _ = VerificationPolicyScheduler.Schedule(ExperimentPolicy.P3_ALWAYS, routes, requests);

        AssertSequenceEqual(routeSnapshot, routes, "routes must remain immutable");
        AssertSequenceEqual(requestSnapshot, requests, "requests must remain immutable");
    }

    private static void SelectiveIsSubsetOfAlwaysForSameBoundary()
    {
        var routes = Routes();
        var requests = Requests(KnownCoreVerifierRules.AirContract, KnownCoreCompilerFacts.AirVerified);
        var selective = VerificationPolicyScheduler.Schedule(ExperimentPolicy.P2_SELECTIVE, routes, requests)
            .Select(static item => item.RuleId)
            .ToHashSet();
        var always = VerificationPolicyScheduler.Schedule(ExperimentPolicy.P3_ALWAYS, routes, requests)
            .Select(static item => item.RuleId)
            .ToHashSet();
        AssertTrue(selective.IsSubsetOf(always), "P2 routes must be a subset of P3 routes at the same boundary");
    }

    private static IReadOnlyList<VerifierRouteDescriptor> Routes() =>
    [
        new(KnownCoreVerifierRules.BytecodeContract, "core.bytecode"),
        new(KnownCoreVerifierRules.AirContract, "core.air")
    ];

    private static IReadOnlyList<ReverificationRequest> Requests(VerifierRuleId rule, CompilerFactId fact) =>
        [new ReverificationRequest(rule, [fact])];

    private static void AssertCount<T>(int expected, IReadOnlyCollection<T> actual)
    {
        if (actual.Count != expected)
            throw new InvalidOperationException($"Policy scheduler self-test failed: expected count {expected}, got {actual.Count}.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("Policy scheduler self-test failed: " + message);
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Policy scheduler self-test failed ({message}): expected '{expected}', got '{actual}'.");
    }

    private static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string message)
    {
        if (!expected.SequenceEqual(actual))
            throw new InvalidOperationException(
                $"Policy scheduler self-test failed ({message}): expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}].");
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Policy scheduler self-test failed: expected {typeof(TException).Name}.");
    }
}
