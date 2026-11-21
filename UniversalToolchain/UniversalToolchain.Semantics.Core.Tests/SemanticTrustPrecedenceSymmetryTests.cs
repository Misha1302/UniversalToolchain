using UniversalToolchain.Semantics.Core;

namespace UniversalToolchain.Semantics.Core.Tests;

/// <summary>
/// Trust precedence must be applied identically whether the disagreement is between a positive and a
/// negative assertion or between two values of a functional predicate.
/// <para>
/// The two paths used to disagree. A positive/negative disagreement at unequal trust produced
/// <c>UTL2407</c> and returned the winning judgment. A functional disagreement at unequal trust returned
/// <c>Proved</c> with <b>no diagnostic at all</b> - and, worse, also returned <c>Proved</c> when asked about
/// the losing value directly. A consumer that only sees "Proved" cannot tell that another provider asserted
/// a different value for the same key, which is exactly the unsound-but-confident answer the core exists to
/// prevent.
/// </para>
/// </summary>
[TestFixture]
public sealed class SemanticTrustPrecedenceSymmetryTests
{
    private static readonly SemanticPredicateId Range = SpikeVocabulary.Range;
    private static readonly SemanticScopeId Scope = SpikeVocabulary.Scope;

    private static SemanticKnowledgeBase NewKb() => new(SpikeVocabulary.Catalog());

    private static SemanticFact RangeFact(
        string low,
        string high,
        string provider,
        SemanticTrust trust,
        CompilationEpoch epoch) =>
        new(
            SpikeVocabulary.Proposition(Range, "v", low, high),
            new SemanticFactContext(Scope, epoch, new SemanticProviderId(provider)),
            trust: trust);

    [Test]
    public void Query_FunctionalDisagreementAtUnequalTrust_ReportsThePrecedenceItApplied()
    {
        var kb = NewKb();
        kb.AddFact(RangeFact("3", "9", "test.package-a", SemanticTrust.Verified, kb.Epoch));
        kb.AddFact(RangeFact("100", "200", "test.package-b", SemanticTrust.Unchecked, kb.Epoch));

        var answer = kb.Ask(SpikeVocabulary.Proposition(Range, "v", "3", "9"));

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Proved));
            Assert.That(answer.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(SemanticDiagnosticCodes.TrustPrecedence),
                "the same rule that reports precedence for positive/negative assertions must report it " +
                "for functional disagreements; silence hides a real cross-provider conflict");
        });
    }

    [Test]
    public void Query_LosingFunctionalValue_IsNotReportedAsProved()
    {
        // The losing tuple was never established as *the* value for this key. Answering "Proved" for it
        // tells a consumer that "v is in [100,200]" holds, while the winning provider says it does not.
        var kb = NewKb();
        kb.AddFact(RangeFact("3", "9", "test.package-a", SemanticTrust.Verified, kb.Epoch));
        kb.AddFact(RangeFact("100", "200", "test.package-b", SemanticTrust.Unchecked, kb.Epoch));

        var losing = kb.Ask(SpikeVocabulary.Proposition(Range, "v", "100", "200"));

        Assert.Multiple(() =>
        {
            Assert.That(losing.Judgment, Is.Not.EqualTo(Judgment.Proved),
                "a value that lost on trust must not be reported as proved");
            Assert.That(losing.Proof, Is.Null,
                "there is no witness for a value the trusted provider contradicts");
            Assert.That(losing.Diagnostics, Is.Not.Empty,
                "an answer that refuses must say why");
        });
    }

    [Test]
    public void Query_FunctionalDisagreementAtEqualTrust_StaysInconsistent()
    {
        // The negative half: the fix must not turn a genuine equal-trust contradiction into a mere
        // precedence warning.
        var kb = NewKb();
        kb.AddFact(RangeFact("3", "9", "test.package-a", SemanticTrust.Verified, kb.Epoch));
        kb.AddFact(RangeFact("100", "200", "test.package-b", SemanticTrust.Verified, kb.Epoch));

        var answer = kb.Ask(SpikeVocabulary.Proposition(Range, "v", "3", "9"));

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Inconsistent));
            Assert.That(answer.Proof, Is.Null);
        });
    }

    [Test]
    public void Query_SingleFunctionalValue_StaysSilentAndProved()
    {
        // The guard against over-reporting: one value, one provider, no disagreement, no diagnostic. Without
        // this, "always emit UTL2407" would pass the tests above while making every ordinary answer noisy.
        var kb = NewKb();
        kb.AddFact(RangeFact("3", "9", "test.package-a", SemanticTrust.Verified, kb.Epoch));

        var answer = kb.Ask(SpikeVocabulary.Proposition(Range, "v", "3", "9"));

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Proved));
            Assert.That(answer.Diagnostics, Is.Empty,
                "an uncontested value must not produce a precedence warning");
        });
    }

    [Test]
    public void Query_PositiveNegativeDisagreementAtUnequalTrust_StillReportsPrecedence()
    {
        // Baseline for the comparison: this path already behaved correctly, and the symmetry fix must not
        // regress it.
        var question = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, "call#1", "lambda#1");
        var kb = NewKb();
        kb.AddFact(new SemanticFact(
            question,
            new SemanticFactContext(Scope, kb.Epoch, new SemanticProviderId("test.trusted")),
            trust: SemanticTrust.Verified));
        kb.AddFact(new SemanticFact(
            question,
            new SemanticFactContext(Scope, kb.Epoch, new SemanticProviderId("test.unchecked")),
            isNegative: true,
            trust: SemanticTrust.Unchecked));

        var answer = kb.Ask(question);

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Proved));
            Assert.That(answer.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(SemanticDiagnosticCodes.TrustPrecedence));
        });
    }

    [Test]
    public void Query_TwoProvidersAgreeingOnOneFunctionalValue_StaySilentAndProved()
    {
        // The second half of the anti-noise guard. Two providers at the *same* trust asserting the *same*
        // value is agreement, not precedence and not a contradiction. Reaching this case requires at least two
        // facts, so the single-value test does not cover it: a mutant that reports precedence whenever no
        // value loses survived until this test existed.
        var kb = NewKb();
        kb.AddFact(RangeFact("3", "9", "test.package-a", SemanticTrust.Verified, kb.Epoch));
        kb.AddFact(RangeFact("3", "9", "test.package-b", SemanticTrust.Verified, kb.Epoch));

        var answer = kb.Ask(SpikeVocabulary.Proposition(Range, "v", "3", "9"));

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Proved));
            Assert.That(answer.Diagnostics, Is.Empty,
                "agreement is not a conflict and must not produce a precedence warning");
        });
    }

    [Test]
    public void Query_Closure_KeepsOneWitnessPerProposition()
    {
        // This is the invariant that makes the `losing.Length == 0` guard in the resolver unreachable: the
        // closure stores one winner per proposition, so two entries with the same key always carry at least
        // two distinct values. Pinned here because the guard's comments claim it; a comment that asserts an
        // invariant no test observes is a false claim waiting to rot.
        var kb = NewKb();
        kb.AddFact(RangeFact("3", "9", "test.package-a", SemanticTrust.Verified, kb.Epoch));
        kb.AddFact(RangeFact("3", "9", "test.package-b", SemanticTrust.Unchecked, kb.Epoch));

        // Two providers, identical proposition, different trust: still one proposition, so the answer for the
        // agreed value is Proved with no precedence report. If this ever produced a warning, the closure had
        // started keeping duplicate propositions for one key and the resolver's guard became semantic.
        var answer = kb.Ask(SpikeVocabulary.Proposition(Range, "v", "3", "9"));

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Proved));
            Assert.That(answer.Diagnostics, Is.Empty,
                "identical propositions are one proposition; differing trust alone is not a value disagreement");
        });
    }
}
