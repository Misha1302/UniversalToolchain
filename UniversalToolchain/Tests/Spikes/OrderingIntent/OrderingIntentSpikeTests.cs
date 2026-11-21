namespace Tests.Spikes.OrderingIntent;

/// <summary>
/// Spike acceptance tests (UT-A055, red-team R-12).
/// <para>
/// The spike exists to answer one question cheaply, before production work: does ordering intent actually
/// compose without global order knowledge? The tests below are the question, phrased so the answer is
/// observable. Three claims have to hold, and one has to keep failing:
/// </para>
/// <list type="number">
/// <item>N independent passes compose with <b>zero</b> explicit orders;</item>
/// <item>contradictory explicit orders still <b>error</b>;</item>
/// <item>the produced order is deterministic and <b>recorded</b>.</item>
/// </list>
/// <para>
/// The fixture also quantifies the two strategies that exist today, so the verdict compares against
/// something measurable rather than against a feeling.
/// </para>
/// </summary>
[TestFixture]
public sealed class OrderingIntentSpikeTests
{
    [Test]
    public void FourIndependentPasses_ComposeWithZeroExplicitOrders()
    {
        // The exact 08-A-1 shape: four mutually-unaware optimizer passes. Under today's model this fails
        // with UTL2208, because all four default to Order=0 and none may be chosen.
        var contributions = Enumerable.Range(1, 4)
            .Select(index => new OrderedContribution(
                $"spike.optimizer.pass-{index}",
                "optimizers",
                OrderingIntent.Unordered.Instance))
            .ToArray();

        var result = OrderingIntentResolver.Resolve(contributions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OrderingStatus.Resolved),
                "four independent passes must compose without any explicit order");
            Assert.That(result.Steps, Has.Count.EqualTo(4));
            Assert.That(result.Policy, Is.Not.Null,
                "the resolution must name the policy that decided, otherwise the determinism is hidden");
            Assert.That(result.Steps.Select(static step => step.Id).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(4));
        });
    }

    [Test]
    public void CompositionHoldsForEverySizeFromOneToEight()
    {
        var failures = new List<string>();
        for (var size = 1; size <= 8; size++)
        {
            var contributions = Enumerable.Range(1, size)
                .Select(index => new OrderedContribution(
                    $"spike.optimizer.pass-{index:D2}",
                    "optimizers",
                    OrderingIntent.Unordered.Instance))
                .ToArray();

            var result = OrderingIntentResolver.Resolve(contributions);
            if (result.Status != OrderingStatus.Resolved)
            {
                failures.Add($"N={size}: {result.Status} ({result.FailureDetail})");
                continue;
            }

            if (result.Steps.Count != size)
                failures.Add($"N={size}: emitted {result.Steps.Count} steps");
            if (result.Steps.Select(static step => step.Id).Distinct(StringComparer.Ordinal).Count() != size)
                failures.Add($"N={size}: duplicate step in output");
        }

        Assert.That(failures, Is.Empty, string.Join("; ", failures));
    }

    [Test]
    public void PhaseRelative_OrdersWithoutNamingAnyPeer()
    {
        // A package that wants to run last in a phase declares exactly that. It never learns which other
        // packages exist in the phase — that is the property that removes the n^2 connector problem.
        var contributions = new[]
        {
            new OrderedContribution("spike.optimizer.first", "optimizers",
                new OrderingIntent.PhaseRelative("optimizers", PhasePosition.First)),
            new OrderedContribution("spike.optimizer.middle-a", "optimizers", OrderingIntent.Unordered.Instance),
            new OrderedContribution("spike.optimizer.middle-b", "optimizers", OrderingIntent.Unordered.Instance),
            new OrderedContribution("spike.optimizer.last", "optimizers",
                new OrderingIntent.PhaseRelative("optimizers", PhasePosition.Last))
        };

        var result = OrderingIntentResolver.Resolve(contributions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OrderingStatus.Resolved));
            Assert.That(result.Steps[0].Id, Is.EqualTo("spike.optimizer.first"));
            Assert.That(result.Steps[0].Reason, Is.EqualTo(OrderingReason.PhaseRelative));
            Assert.That(result.Steps[^1].Id, Is.EqualTo("spike.optimizer.last"));
            Assert.That(result.Steps[^1].Reason, Is.EqualTo(OrderingReason.PhaseRelative));
        });
    }

    [Test]
    public void SemanticAnchor_CreatesEdgesOnlyWithinTheSameAnchor()
    {
        // Two packages share an anchor; a third declares nothing and must not be dragged into the anchor's
        // ordering, or anchors would become a global mechanism in disguise.
        var contributions = new[]
        {
            new OrderedContribution("spike.fold.a", "optimizers",
                new OrderingIntent.SemanticAnchor("constant-folding", AnchorPosition.Before)),
            new OrderedContribution("spike.fold.b", "optimizers",
                new OrderingIntent.SemanticAnchor("constant-folding", AnchorPosition.After)),
            new OrderedContribution("spike.unrelated", "optimizers", OrderingIntent.Unordered.Instance)
        };

        var result = OrderingIntentResolver.Resolve(contributions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OrderingStatus.Resolved));
            var order = result.Steps.Select(static step => step.Id).ToList();
            Assert.That(order.IndexOf("spike.fold.a"), Is.LessThan(order.IndexOf("spike.fold.b")),
                "Before/After within one anchor must be honoured");

            Assert.That(result.Steps.Single(step => step.Id == "spike.fold.a").Reason,
                Is.EqualTo(OrderingReason.SemanticAnchor));
            Assert.That(result.Steps.Single(step => step.Id == "spike.fold.b").Reason,
                Is.EqualTo(OrderingReason.SemanticAnchor));
            Assert.That(result.Steps.Single(step => step.Id == "spike.unrelated").Reason,
                Is.EqualTo(OrderingReason.DeclaredPolicy),
                "an unrelated contribution must fall to policy, not to the anchor");
        });
    }

    [Test]
    public void ContradictoryExplicitOrders_StillError()
    {
        // The invariant that must survive the redesign. Silently resolving a contradiction is how a
        // planner turns a package bug into a wrong execution order.
        var contributions = new[]
        {
            new OrderedContribution("spike.pass.a", "optimizers", new OrderingIntent.Explicit(5)),
            new OrderedContribution("spike.pass.b", "optimizers", new OrderingIntent.Explicit(5))
        };

        var result = OrderingIntentResolver.Resolve(contributions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OrderingStatus.ExplicitConflict),
                "equal explicit orders must remain an error, exactly as UTL2208 does today");
            Assert.That(result.Steps, Is.Empty);
            Assert.That(result.FailureDetail, Does.Contain("equal explicit order"));
        });
    }

    [Test]
    public void ExplicitOrderIsStillHonoured()
    {
        // Backwards compatibility: a package that opted into integers keeps today's behaviour.
        var contributions = new[]
        {
            new OrderedContribution("spike.pass.late", "optimizers", new OrderingIntent.Explicit(20)),
            new OrderedContribution("spike.pass.early", "optimizers", new OrderingIntent.Explicit(10))
        };

        var result = OrderingIntentResolver.Resolve(contributions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OrderingStatus.Resolved));
            Assert.That(result.Steps.Select(static step => step.Id),
                Is.EqualTo(new[] { "spike.pass.early", "spike.pass.late" }));
            Assert.That(result.Steps.All(static step => step.Reason == OrderingReason.Explicit));
            Assert.That(result.Steps.All(static step => !step.ChosenByPolicy),
                "two distinct explicit orders are decided by the constraint, not by a tie-break");
        });
    }

    [Test]
    public void ResolutionIsDeterministicAcrossInputPermutations()
    {
        // A resolver whose output depends on input order would move the n^2 problem from declaration to
        // enumeration, which is worse because it is invisible.
        var baseline = new[]
        {
            new OrderedContribution("spike.a", "optimizers", OrderingIntent.Unordered.Instance),
            new OrderedContribution("spike.b", "optimizers", OrderingIntent.Unordered.Instance),
            new OrderedContribution("spike.c", "optimizers", OrderingIntent.Unordered.Instance),
            new OrderedContribution("spike.d", "optimizers", OrderingIntent.Unordered.Instance)
        };

        var expected = OrderingIntentResolver.Resolve(baseline).Steps.Select(static step => step.Id).ToArray();
        var permutations = Permute(baseline);

        var mismatches = permutations
            .Select(permutation => OrderingIntentResolver.Resolve(permutation).Steps.Select(static step => step.Id).ToArray())
            .Where(actual => !actual.SequenceEqual(expected, StringComparer.Ordinal))
            .Select(static actual => string.Join(",", actual))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(permutations, Has.Count.EqualTo(24), "4! permutations should have been produced");
            Assert.That(mismatches, Is.Empty,
                "resolution must not depend on enumeration order: " + string.Join(" | ", mismatches));
        });
    }

    [Test]
    public void AddingAnUnrelatedPackage_DoesNotReorderExistingPasses()
    {
        // Metamorphic property: a new package must not disturb the relative order of the old ones,
        // otherwise every author is coupled to every other author's release timing.
        var baseline = new[]
        {
            new OrderedContribution("spike.a", "optimizers", OrderingIntent.Unordered.Instance),
            new OrderedContribution("spike.b", "optimizers", OrderingIntent.Unordered.Instance),
            new OrderedContribution("spike.c", "optimizers", OrderingIntent.Unordered.Instance)
        };
        var extended = baseline
            .Append(new OrderedContribution("spike.zzz-new", "optimizers", OrderingIntent.Unordered.Instance))
            .ToArray();

        var before = OrderingIntentResolver.Resolve(baseline).Steps.Select(static step => step.Id).ToArray();
        var after = OrderingIntentResolver
            .Resolve(extended)
            .Steps
            .Select(static step => step.Id)
            .Where(id => before.Contains(id, StringComparer.Ordinal))
            .ToArray();

        Assert.That(after, Is.EqualTo(before),
            "relative order of existing passes changed when an unrelated package was added");
    }

    [Test]
    public void TwoMembersDeclaringTheSamePosition_AgreeRatherThanConflict()
    {
        // Agreement must not be mistaken for a contradiction. Two passes that both say "before the
        // constant-folding anchor" agree with each other; inventing an ordering edge between them would
        // turn agreement into a cycle, and a spike that fails here would have rejected a legitimate
        // declaration shape.
        var result = OrderingIntentResolver.Resolve(
        [
            new OrderedContribution("spike.first-a", "optimizers",
                new OrderingIntent.PhaseRelative("optimizers", PhasePosition.First)),
            new OrderedContribution("spike.first-b", "optimizers",
                new OrderingIntent.PhaseRelative("optimizers", PhasePosition.First)),
            new OrderedContribution("spike.neutral", "optimizers", OrderingIntent.Unordered.Instance),
            new OrderedContribution("spike.last", "optimizers",
                new OrderingIntent.PhaseRelative("optimizers", PhasePosition.Last))
        ]);

        Assert.That(result.Status, Is.EqualTo(OrderingStatus.Resolved),
            "two members declaring 'first' in one phase is satisfiable, not a cycle");
        Assert.That(result.Steps.Select(static step => step.Id),
            Is.EqualTo(new[] { "spike.first-a", "spike.first-b", "spike.neutral", "spike.last" }));
    }

    [Test]
    public void NoDeclarationShapeReachesACycle_UnderOneIntentPerContribution()
    {
        // Measured, not assumed. Every intent assignment for populations of 1..4 was enumerated through the
        // resolver and no assignment produced a cycle. The reason is structural: each mechanism only
        // claims edges FROM its own members TO members that declare nothing, so one mechanism can never
        // demand the reverse of another. Explicit edges live entirely inside the explicit set, anchor edges
        // inside one anchor, and phase edges always point member -> neutral.
        //
        // That is a real property with a real cost: a contribution cannot say both "anchor-After" and
        // "last in phase", because it carries exactly one intent. The property is what makes the model
        // safe; the expressiveness limit is what a production PR would have to decide on.
        var intents = new Func<OrderingIntent>[]
        {
            static () => OrderingIntent.Unordered.Instance,
            static () => new OrderingIntent.Explicit(1),
            static () => new OrderingIntent.Explicit(2),
            static () => new OrderingIntent.SemanticAnchor("a", AnchorPosition.Before),
            static () => new OrderingIntent.SemanticAnchor("a", AnchorPosition.After),
            static () => new OrderingIntent.PhaseRelative("p", PhasePosition.First),
            static () => new OrderingIntent.PhaseRelative("p", PhasePosition.Last)
        };

        var observed = new HashSet<OrderingStatus>();
        var cyclicShapes = new List<string>();
        for (var size = 1; size <= 4; size++)
        {
            var total = (int)Math.Pow(intents.Length, size);
            for (var combination = 0; combination < total; combination++)
            {
                var items = new OrderedContribution[size];
                var remainder = combination;
                for (var index = 0; index < size; index++)
                {
                    items[index] = new OrderedContribution($"c{index}", "p", intents[remainder % intents.Length]());
                    remainder /= intents.Length;
                }

                var result = OrderingIntentResolver.Resolve(items);
                observed.Add(result.Status);
                if (result.Status == OrderingStatus.Cycle && cyclicShapes.Count < 3)
                    cyclicShapes.Add(string.Join(",", items.Select(static item => item.Intent.ToString())));
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(cyclicShapes, Is.Empty,
                "a cycle was reachable, so the structural claim above is wrong: " + string.Join(" | ", cyclicShapes));
            Assert.That(observed, Does.Contain(OrderingStatus.Resolved));
            Assert.That(observed, Does.Contain(OrderingStatus.ExplicitConflict),
                "the equal-order refusal must still be reachable, or the check is dead code");
        });
    }

    [Test]
    public void TheDeclaredOrderIsRecordedPerStep_SoReviewCanObject()
    {
        // A plan step that cannot explain itself is not reviewable. Every emitted step carries the reason
        // its constraints exist, and whether policy chose it among ties.
        var result = OrderingIntentResolver.Resolve(
        [
            new OrderedContribution("spike.anchored", "optimizers",
                new OrderingIntent.SemanticAnchor("fold", AnchorPosition.Before)),
            new OrderedContribution("spike.free-a", "optimizers", OrderingIntent.Unordered.Instance),
            new OrderedContribution("spike.free-b", "optimizers", OrderingIntent.Unordered.Instance)
        ]);

        var anchored = result.Steps.Single(static step => step.Id == "spike.anchored");
        var free = result.Steps.Where(static step => step.Id != "spike.anchored").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OrderingStatus.Resolved));
            Assert.That(anchored.Reason, Is.EqualTo(OrderingReason.SemanticAnchor));
            Assert.That(anchored.Explanation, Does.Contain("fold"));
            Assert.That(free.All(static step => step.Reason == OrderingReason.DeclaredPolicy));
            var policyKind = result.Policy!.Kind;
            Assert.That(free.All(step => step.Explanation.Contains(policyKind, StringComparison.Ordinal)),
                "a policy decision must name the policy that made it, otherwise it is hidden determinism");
            Assert.That(free.Any(static step => step.ChosenByPolicy),
                "two free contributions must have been decided by the policy for at least one of them");
        });
    }

    [Test]
    public void DuplicateContributionId_IsRejectedBeforeAnyOrdering()
    {
        var result = OrderingIntentResolver.Resolve(
        [
            new OrderedContribution("spike.same", "optimizers", OrderingIntent.Unordered.Instance),
            new OrderedContribution("spike.same", "optimizers", new OrderingIntent.Explicit(7))
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OrderingStatus.DuplicateId));
            Assert.That(result.FailureDetail, Does.Contain("spike.same"));
        });
    }

    [Test]
    public void AnchorAndPhaseEdges_Together_ProduceTheDeclaredOrder()
    {
        // Composition of the two mechanisms on one contribution, which is what a real package would do.
        var result = OrderingIntentResolver.Resolve(
        [
            new OrderedContribution("spike.fold", "optimizers",
                new OrderingIntent.SemanticAnchor("constant-folding", AnchorPosition.Before)),
            new OrderedContribution("spike.propagate", "optimizers",
                new OrderingIntent.SemanticAnchor("constant-folding", AnchorPosition.After)),
            new OrderedContribution("spike.guard", "optimizers",
                new OrderingIntent.PhaseRelative("optimizers", PhasePosition.Last))
        ]);

        Assert.That(result.Status, Is.EqualTo(OrderingStatus.Resolved));
        Assert.That(result.Steps.Select(static step => step.Id),
            Is.EqualTo(new[] { "spike.fold", "spike.propagate", "spike.guard" }));
    }

    [Test]
    public void TodayStrategy_QuantifiedForComparison()
    {
        // The verdict depends on comparing against what exists now, not on a feeling. Under today's model
        // N independent passes at Order=0 are indistinguishable, which is exactly what UTL2208 reports.
        // This test records the arithmetic behind the verdict.
        const int size = 8;
        var integersNeeded = size;
        var edgesIfEveryAuthorConnectsToEveryPeer = size * (size - 1) / 2;

        Assert.Multiple(() =>
        {
            Assert.That(integersNeeded, Is.EqualTo(size));
            Assert.That(edgesIfEveryAuthorConnectsToEveryPeer, Is.EqualTo(28),
                "28 pairwise edges at N=8 is the n^2 cost the article describes");
            Assert.That(edgesIfEveryAuthorConnectsToEveryPeer,
                Is.GreaterThan(integersNeeded),
                "pairwise edges cost more than integers at N=8, but both require global knowledge");
        });
    }

    private static List<OrderedContribution[]> Permute(OrderedContribution[] items)
    {
        var results = new List<OrderedContribution[]>();

        void Walk(List<OrderedContribution> current, List<OrderedContribution> remaining)
        {
            if (remaining.Count == 0)
            {
                results.Add([.. current]);
                return;
            }

            for (var index = 0; index < remaining.Count; index++)
            {
                var next = remaining[index];
                current.Add(next);
                var rest = remaining.Where((_, position) => position != index).ToList();
                Walk(current, rest);
                current.RemoveAt(current.Count - 1);
            }
        }

        Walk([], [.. items]);
        return results;
    }
}
