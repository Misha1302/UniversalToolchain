using UniversalToolchain.Semantics.Core;

namespace UniversalToolchain.Semantics.Core.TestFixtures;

/// <summary>
/// A deliberately small devirtualizing optimizer used by the spike.
/// </summary>
/// <remarks>
/// <para>It receives only an <see cref="ISemanticQuery"/>. It has no idea which module published the
/// <c>ExactTarget</c> fact, and it cannot see any type from that module — the project file proves it.</para>
/// <para>Every judgment is handled explicitly. <see cref="Judgment.Proved"/> is the only state that
/// permits a rewrite; <see cref="Judgment.Inconsistent"/> is a hard refusal rather than a silent skip,
/// because two providers disagreeing is a defect the author must see.</para>
/// </remarks>
public sealed class DevirtualizationOptimizer
{
    public static readonly SemanticPredicateId ExactTargetPredicate = new("ExactTarget", 2);

    private readonly ISemanticQuery _query;

    public DevirtualizationOptimizer(ISemanticQuery query) =>
        _query = query ?? throw new ArgumentNullException(nameof(query));

    public IReadOnlyCollection<SemanticProposition> RequiredObligations(
        IEnumerable<(SemanticEntityId CallSite, SemanticEntityId Target)> candidates) =>
        candidates
            .Select(static candidate => new SemanticProposition(
                ExactTargetPredicate,
                candidate.CallSite,
                candidate.Target))
            .ToArray();

    /// <summary>
    /// Decides whether the call site may be replaced by a direct call to <paramref name="candidateTarget"/>.
    /// </summary>
    public OptimizationDecision Decide(SemanticEntityId callSite, SemanticEntityId candidateTarget)
    {
        var obligation = new SemanticProposition(ExactTargetPredicate, callSite, candidateTarget);
        var answer = _query.Ask(obligation);

        return answer.Judgment switch
        {
            Judgment.Proved => new OptimizationDecision(
                OptimizationOutcome.Apply,
                obligation,
                answer,
                $"Exact target is proved for '{callSite}': the call may be devirtualized to '{candidateTarget}'."),
            Judgment.Disproved => new OptimizationDecision(
                OptimizationOutcome.Skipped,
                obligation,
                answer,
                $"Exact target is disproved for '{callSite}': the call cannot be devirtualized to '{candidateTarget}'."),
            Judgment.Unknown => new OptimizationDecision(
                OptimizationOutcome.Skipped,
                obligation,
                answer,
                $"Exact target is unknown for '{callSite}': no provider established '{obligation.Predicate}'."),
            Judgment.Inconsistent => new OptimizationDecision(
                OptimizationOutcome.Refused,
                obligation,
                answer,
                $"Providers disagree about '{obligation}': refusing the transformation instead of guessing."),
            _ => throw new ArgumentOutOfRangeException(nameof(candidateTarget), answer.Judgment, "Unhandled judgment.")
        };
    }
}

public enum OptimizationOutcome
{
    /// <summary>The proof holds; the rewrite is admissible.</summary>
    Apply,

    /// <summary>The proof does not hold; the optimizer leaves the call alone.</summary>
    Skipped,

    /// <summary>The knowledge base contradicts itself; the optimizer refuses and reports a defect.</summary>
    Refused
}

public sealed record OptimizationDecision(
    OptimizationOutcome Outcome,
    SemanticProposition Obligation,
    SemanticAnswer Answer,
    string Reason)
{
    public bool Rewrites => Outcome == OptimizationOutcome.Apply;
}
