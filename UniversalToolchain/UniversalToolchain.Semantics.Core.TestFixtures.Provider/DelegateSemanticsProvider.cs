using UniversalToolchain.Semantics.Core;

namespace UniversalToolchain.Semantics.Core.TestFixtures.Provider;

/// <summary>
/// Stands in for the package that knows which delegate/lambda pairs are exact.
/// </summary>
/// <remarks>
/// This provider publishes only facts it owns, stamped with the context the engine supplied. It cannot
/// see the optimizer, cannot select a contribution, and cannot read the program.
/// </remarks>
public sealed class DelegateSemanticsProvider : ISemanticFactProvider, ISemanticRuleProvider
{
    public static readonly SemanticProviderId Provider = new("test.delegates");
    public static readonly SemanticPredicateId ExactTargetPredicate = new("ExactTarget", 2);
    public static readonly SemanticPredicateId CapturesNothingPredicate = new("CapturesNothing", 1);
    public static readonly SemanticPredicateId SameValuePredicate = new("SameValue", 2);

    private readonly IReadOnlyList<(SemanticEntityId CallSite, SemanticEntityId Target)> _exactTargets;
    private readonly IReadOnlyCollection<SemanticEntityId> _captureFree;
    private readonly SemanticTrust _trust;

    public DelegateSemanticsProvider(
        IEnumerable<(SemanticEntityId CallSite, SemanticEntityId Target)>? exactTargets = null,
        IEnumerable<SemanticEntityId>? captureFree = null,
        SemanticTrust trust = SemanticTrust.Verified)
    {
        _exactTargets = (exactTargets ?? []).ToArray();
        _captureFree = (captureFree ?? []).ToArray();
        _trust = trust;
    }

    public SemanticProviderId Id => Provider;

    public IReadOnlyCollection<SemanticFact> DeclarePrimitiveFacts(SemanticFactContext context)
    {
        var facts = new List<SemanticFact>();

        foreach (var (callSite, target) in _exactTargets)
        {
            facts.Add(new SemanticFact(
                new SemanticProposition(ExactTargetPredicate, callSite, target),
                context,
                trust: _trust));
        }

        foreach (var entity in _captureFree)
        {
            facts.Add(new SemanticFact(
                new SemanticProposition(CapturesNothingPredicate, entity),
                context,
                trust: _trust));
        }

        return facts;
    }

    /// <summary>
    /// One rule: a capture-free target that is the same value as the call site's receiver is an exact
    /// target. This exists to exercise the derived-evidence path, not to be realistic.
    /// </summary>
    public IReadOnlyCollection<SemanticRule> Rules =>
    [
        new SemanticRule(
            Provider,
            new SemanticProposition(ExactTargetPredicate, new SemanticEntityId("call#7"), new SemanticEntityId("lambda#7")),
            [
                new SemanticProposition(CapturesNothingPredicate, new SemanticEntityId("lambda#7")),
                new SemanticProposition(SameValuePredicate, new SemanticEntityId("receiver#7"), new SemanticEntityId("lambda#7"))
            ],
            SemanticTrust.Verified)
    ];
}
