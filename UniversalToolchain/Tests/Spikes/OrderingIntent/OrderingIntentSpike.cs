namespace Tests.Spikes.OrderingIntent;

/// <summary>
/// Spike (UT-A055, red-team R-12): ordering intent union.
/// <para>
/// This type exists <em>only</em> to answer a design question before production work: can contributions
/// declare local ordering intent and still compose, without anyone knowing a global order number? It is
/// deliberately not referenced by the planner. Nothing here may be mistaken for an implemented feature —
/// the spike's verdict decides whether the production PR is worth starting at all.
/// </para>
/// </summary>
internal abstract record OrderingIntent
{
    private OrderingIntent()
    {
    }

    /// <summary>Today's behaviour: a global integer. Kept unchanged for existing packages.</summary>
    public sealed record Explicit(int Order) : OrderingIntent;

    /// <summary>
    /// Relates to a named semantic anchor rather than to every peer. Edges are produced only against
    /// contributions that declare the same anchor, so the author needs no knowledge of other packages.
    /// </summary>
    public sealed record SemanticAnchor(string AnchorId, AnchorPosition Position) : OrderingIntent;

    /// <summary>Relates to a phase boundary: run first or last inside this phase.</summary>
    public sealed record PhaseRelative(string Phase, PhasePosition Position) : OrderingIntent;

    /// <summary>Participates in the declared tie-break policy. Not "no order" — "the policy decides".</summary>
    public sealed record Unordered : OrderingIntent
    {
        public static Unordered Instance { get; } = new();
    }
}

/// <summary>Where a semantic anchor places a contribution relative to the other members of that anchor.</summary>
internal enum AnchorPosition
{
    Before,
    After
}

/// <summary>Where a phase-relative contribution places itself inside its phase.</summary>
internal enum PhasePosition
{
    First,
    Last
}

/// <summary>A contribution as the resolver sees it: identity, phase, and its declared intent.</summary>
internal sealed record OrderedContribution(string Id, string Phase, OrderingIntent Intent);

/// <summary>Why a contribution carries the ordering constraints it carries. Recorded so review can object.</summary>
internal enum OrderingReason
{
    Explicit,
    SemanticAnchor,
    PhaseRelative,
    DeclaredPolicy
}

/// <summary>
/// One emitted step.
/// <para>
/// <see cref="Reason"/> is the origin of the step's <em>declared</em> constraints.
/// <see cref="ChosenByPolicy"/> records that, at the moment this step became ready, it was tied with a
/// peer and the declared tie-break policy picked it. Keeping the two apart matters: a contribution can
/// be anchor-constrained <em>and</em> have been chosen ahead of an unrelated peer by policy, and folding
/// that into one field would misreport which decision a reviewer should look at.
/// </para>
/// </summary>
internal sealed record ResolvedStep(
    string Id,
    OrderingReason Reason,
    string Explanation,
    bool ChosenByPolicy);

/// <summary>The tie-break policy is named in the result, never implied.</summary>
internal sealed record TieBreakPolicy(string Kind)
{
    /// <summary>
    /// Ordinal by contribution id. Deterministic and declared, which is what the project law permits:
    /// it forbids <em>hidden</em> determinism, not declared determinism.
    /// </summary>
    public static TieBreakPolicy OrdinalContributionId { get; } = new("contribution-id-ordinal");
}

internal enum OrderingStatus
{
    Resolved,
    ExplicitConflict,
    DuplicateId,
    Cycle
}

internal sealed record OrderingResult(
    OrderingStatus Status,
    IReadOnlyList<ResolvedStep> Steps,
    TieBreakPolicy? Policy,
    string? FailureDetail)
{
    public static OrderingResult Fail(OrderingStatus status, string detail) =>
        new(status, [], null, detail);
}

/// <summary>
/// Resolves declared ordering intent into a total order, without any global order knowledge.
/// </summary>
internal static class OrderingIntentResolver
{
    public static OrderingResult Resolve(
        IEnumerable<OrderedContribution> contributions,
        TieBreakPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        policy ??= TieBreakPolicy.OrdinalContributionId;

        var items = contributions.ToArray();
        if (items.Length == 0)
            return new OrderingResult(OrderingStatus.Resolved, [], policy, null);

        var duplicated = items
            .GroupBy(static item => item.Id, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();
        if (duplicated.Length != 0)
        {
            return OrderingResult.Fail(
                OrderingStatus.DuplicateId,
                $"duplicate contribution id(s): {string.Join(", ", duplicated)}");
        }

        // `requires[id]` is the set of ids that must already be emitted before `id` may be emitted.
        // Storing the relation this way makes readiness a single, hard-to-invert check: an item is ready
        // when every element of its requirement set is done.
        var requires = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var reasons = new Dictionary<string, (OrderingReason Reason, string Explanation)>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            requires[item.Id] = [];
            reasons[item.Id] = item.Intent switch
            {
                OrderingIntent.Explicit explicitIntent
                    => (OrderingReason.Explicit, $"explicit order {explicitIntent.Order}"),
                OrderingIntent.SemanticAnchor anchor
                    => (OrderingReason.SemanticAnchor, $"anchor '{anchor.AnchorId}' {anchor.Position}"),
                OrderingIntent.PhaseRelative phase
                    => (OrderingReason.PhaseRelative, $"phase '{phase.Phase}' {phase.Position}"),
                _ => (OrderingReason.DeclaredPolicy, $"declared policy {policy.Kind}")
            };
        }

        // Explicit: a lower order runs first. This is a hard constraint, not a tie-break. Equal explicit
        // orders stay an error, exactly as UTL2208 does today.
        var explicitItems = items
            .Where(static item => item.Intent is OrderingIntent.Explicit)
            .Select(static item => (item.Id, Order: ((OrderingIntent.Explicit)item.Intent).Order))
            .ToArray();
        foreach (var (leftId, leftOrder) in explicitItems)
        {
            foreach (var (rightId, rightOrder) in explicitItems)
            {
                if (StringComparer.Ordinal.Equals(leftId, rightId))
                    continue;
                if (leftOrder == rightOrder)
                {
                    return OrderingResult.Fail(
                        OrderingStatus.ExplicitConflict,
                        $"equal explicit order {leftOrder} for '{leftId}' and '{rightId}'");
                }

                if (leftOrder < rightOrder)
                    requires[rightId].Add(leftId);
            }
        }

        // Anchor: relations only against contributions that name the same anchor. Nobody has to know every
        // peer — that is the whole point, and the acceptance criterion for the spike.
        foreach (var group in items
                     .Where(static item => item.Intent is OrderingIntent.SemanticAnchor)
                     .GroupBy(static item => ((OrderingIntent.SemanticAnchor)item.Intent).AnchorId, StringComparer.Ordinal))
        {
            var anchored = group.ToArray();
            var beforeMembers = anchored
                .Where(static item => ((OrderingIntent.SemanticAnchor)item.Intent).Position == AnchorPosition.Before)
                .ToArray();
            var afterMembers = anchored
                .Where(static item => ((OrderingIntent.SemanticAnchor)item.Intent).Position == AnchorPosition.After)
                .ToArray();

            // Same rule as phases: members declaring the SAME position are not ordered against each
            // other. Two contributions that both say "before constant-folding" agree with each other; they
            // do not claim to precede one another, and inventing an edge between them would turn agreement
            // into a cycle. Their mutual order is left to the declared tie-break.
            var neutralMembers = anchored
                .Where(item => !beforeMembers.Any(peer => StringComparer.Ordinal.Equals(peer.Id, item.Id)))
                .Where(item => !afterMembers.Any(peer => StringComparer.Ordinal.Equals(peer.Id, item.Id)))
                .ToArray();

            foreach (var member in beforeMembers)
            {
                foreach (var peer in afterMembers)
                    requires[peer.Id].Add(member.Id);
                foreach (var peer in neutralMembers)
                    requires[peer.Id].Add(member.Id);
            }

            foreach (var member in afterMembers)
            {
                foreach (var peer in neutralMembers)
                    requires[member.Id].Add(peer.Id);
                foreach (var peer in beforeMembers)
                    requires[member.Id].Add(peer.Id);
            }
        }

        // Phase-relative: a relation against every member of the named phase, without naming the members.
        // Members that declare the SAME position are not ordered against each other - ordering two
        // "first" members would be a contradiction, and resolving it by declaration order is exactly the
        // global knowledge this spike is trying to remove. They are left to the declared tie-break.
        foreach (var phaseGroup in items
                     .Where(static item => item.Intent is OrderingIntent.PhaseRelative)
                     .GroupBy(static item => ((OrderingIntent.PhaseRelative)item.Intent).Phase, StringComparer.Ordinal))
        {
            var phase = phaseGroup.Key;
            var declaring = phaseGroup.ToArray();
            var first = declaring.Where(static item => ((OrderingIntent.PhaseRelative)item.Intent).Position == PhasePosition.First).ToArray();
            var last = declaring.Where(static item => ((OrderingIntent.PhaseRelative)item.Intent).Position == PhasePosition.Last).ToArray();
            var neutral = items
                .Where(item => item.Phase == phase)
                .Where(item => declaring.All(peer => !StringComparer.Ordinal.Equals(peer.Id, item.Id)))
                .ToArray();

            foreach (var member in first)
            {
                foreach (var peer in neutral)
                    requires[peer.Id].Add(member.Id);
                foreach (var peer in last)
                    requires[peer.Id].Add(member.Id);
            }

            foreach (var member in last)
            {
                foreach (var peer in neutral)
                    requires[member.Id].Add(peer.Id);
                foreach (var peer in first)
                    requires[member.Id].Add(peer.Id);
            }
        }

        return Topological(requires, items, reasons, policy);
    }

    private static OrderingResult Topological(
        Dictionary<string, HashSet<string>> requires,
        OrderedContribution[] items,
        Dictionary<string, (OrderingReason Reason, string Explanation)> reasons,
        TieBreakPolicy policy)
    {
        var emitted = new List<ResolvedStep>(items.Length);
        var done = new HashSet<string>(StringComparer.Ordinal);
        var explicitOrderById = items.ToDictionary(
            static item => item.Id,
            static item => item.Intent is OrderingIntent.Explicit explicitIntent ? explicitIntent.Order : int.MinValue,
            StringComparer.Ordinal);

        while (done.Count != items.Length)
        {
            var ready = items
                .Where(item => !done.Contains(item.Id))
                .Where(item => requires[item.Id].All(done.Contains))
                .ToArray();

            if (ready.Length == 0)
            {
                var blocked = items
                    .Where(item => !done.Contains(item.Id))
                    .Select(static item => item.Id)
                    .ToArray();
                return OrderingResult.Fail(
                    OrderingStatus.Cycle,
                    $"declared constraints contain a cycle among: {string.Join(", ", blocked)}");
            }

            // The only place a tie-break happens, and it is the declared one.
            var chosen = ready
                .OrderBy(item => explicitOrderById[item.Id])
                .ThenBy(static item => item.Id, StringComparer.Ordinal)
                .First();

            var lowestExplicit = ready.Min(item => explicitOrderById[item.Id]);
            var tied = ready.Length > 1 && ready.Count(item => explicitOrderById[item.Id] == lowestExplicit) > 1;

            var (reason, explanation) = reasons[chosen.Id];
            emitted.Add(new ResolvedStep(chosen.Id, reason, explanation, tied));
            done.Add(chosen.Id);
        }

        return new OrderingResult(OrderingStatus.Resolved, emitted, policy, null);
    }
}
