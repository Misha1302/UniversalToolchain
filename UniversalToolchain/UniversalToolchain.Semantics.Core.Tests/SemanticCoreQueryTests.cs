using UniversalToolchain.Semantics.Core;
using UniversalToolchain.Semantics.Core.TestFixtures;
using UniversalToolchain.Semantics.Core.TestFixtures.Provider;

namespace UniversalToolchain.Semantics.Core.Tests;

/// <summary>
/// UT-A026 scenarios 1-5 plus the negative and determinism contracts from §6 of the feature file.
/// </summary>
[TestFixture]
public sealed class SemanticCoreQueryTests
{
    private static readonly SemanticEntityId Call = new("call#1");
    private static readonly SemanticEntityId Lambda = new("lambda#1");

    private static SemanticKnowledgeBase NewKb(int maxDepth = 8) =>
        new(SpikeVocabulary.Catalog(), maxDerivationDepth: maxDepth);

    [Test]
    public void Query_ExactTarget_ProvedFromIndependentProvider()
    {
        var provider = new DelegateSemanticsProvider([(Call, Lambda)]);
        var kb = NewKb().AddProvider(provider, SpikeVocabulary.Scope);
        var question = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);

        var answer = kb.Ask(question);

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Proved));
            Assert.That(answer.Proof, Is.Not.Null);
            Assert.That(answer.Proof!.Origin, Is.EqualTo(EvidenceOrigin.DeclaredByOwner));
            Assert.That(answer.Proof.Provider, Is.EqualTo(DelegateSemanticsProvider.Provider));
            Assert.That(answer.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Query_MissingFact_IsUnknown_NotDisproved()
    {
        // Nothing is published. In particular, no provider asserts the negative either.
        var kb = NewKb();
        var question = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);

        var answer = kb.Ask(question);

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Unknown),
                "Absence of evidence must never be reported as a disproof; negation-as-failure is out of scope in v1.");
            Assert.That(answer.Judgment, Is.Not.EqualTo(Judgment.Disproved));
            Assert.That(answer.Proof, Is.Null);
        });
    }

    [Test]
    public void Query_ExplicitNegativeAxiom_IsDisproved()
    {
        var kb = NewKb();
        var question = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);
        kb.AddFact(new SemanticFact(
            question,
            new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, DelegateSemanticsProvider.Provider),
            isNegative: true));

        var answer = kb.Ask(question);

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Disproved));
            Assert.That(answer.Proof, Is.Not.Null);
            Assert.That(answer.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(SemanticDiagnosticCodes.ObligationDisproved));
        });
    }

    [Test]
    public void Query_ContradictoryAxioms_IsInconsistent()
    {
        // Scenario 3: two packages publish different ranges for the same variable.
        var kb = NewKb();
        var scope = SpikeVocabulary.Scope;
        var variable = new SemanticEntityId("v");

        kb.AddFact(new SemanticFact(
            new SemanticProposition(SpikeVocabulary.Range, variable, new SemanticEntityId("3"), new SemanticEntityId("9")),
            new SemanticFactContext(scope, kb.Epoch, new SemanticProviderId("test.package-a"))));

        kb.AddFact(new SemanticFact(
            new SemanticProposition(SpikeVocabulary.Range, variable, new SemanticEntityId("100"), new SemanticEntityId("200")),
            new SemanticFactContext(scope, kb.Epoch, new SemanticProviderId("test.package-b"))));

        var answer = kb.Ask(SpikeVocabulary.Proposition(SpikeVocabulary.Range, "v", "3", "9"));

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Inconsistent),
                "Two providers disagreeing is a configuration defect and must not be folded into Unknown.");
            Assert.That(answer.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(SemanticDiagnosticCodes.ObligationInconsistent));
        });
    }

    [Test]
    public void Query_ContradictoryAxioms_AreNotReportedWhenOneSideIsStale()
    {
        var kb = NewKb();
        var variable = new SemanticEntityId("v");
        var first = new SemanticProposition(SpikeVocabulary.Range, variable, new SemanticEntityId("3"), new SemanticEntityId("9"));
        var second = new SemanticProposition(SpikeVocabulary.Range, variable, new SemanticEntityId("100"), new SemanticEntityId("200"));

        kb.AddFact(new SemanticFact(first, new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, new SemanticProviderId("test.package-a"))));
        kb.AddFact(new SemanticFact(second, new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, new SemanticProviderId("test.package-b"))));

        Assert.That(kb.Ask(first).Judgment, Is.EqualTo(Judgment.Inconsistent));

        kb.AdvanceEpoch();
        var afterBump = kb.Ask(first);

        Assert.Multiple(() =>
        {
            Assert.That(afterBump.Judgment, Is.EqualTo(Judgment.Unknown),
                "Both contradictory facts are stale, so there is nothing to contradict any more.");
            Assert.That(afterBump.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(SemanticDiagnosticCodes.EvidenceStale));
        });
    }

    [Test]
    public void Query_EpochBump_Invalidates()
    {
        var provider = new DelegateSemanticsProvider([(Call, Lambda)]);
        var kb = NewKb().AddProvider(provider, SpikeVocabulary.Scope);
        var question = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);

        Assert.That(kb.Ask(question).Judgment, Is.EqualTo(Judgment.Proved));

        kb.AdvanceEpoch();
        var stale = kb.Ask(question);

        Assert.Multiple(() =>
        {
            Assert.That(stale.Judgment, Is.EqualTo(Judgment.Unknown), "A fact from an earlier epoch must not answer a later query.");
            Assert.That(stale.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(SemanticDiagnosticCodes.EvidenceStale));
            Assert.That(stale.Proof, Is.Null);
        });
    }

    [Test]
    public void Query_EpochBump_IsRepairedByReestablishingTheFact()
    {
        var provider = new DelegateSemanticsProvider([(Call, Lambda)]);
        var kb = NewKb().AddProvider(provider, SpikeVocabulary.Scope);
        var question = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);

        kb.AdvanceEpoch();
        Assert.That(kb.Ask(question).Judgment, Is.EqualTo(Judgment.Unknown));

        kb.AddProvider(provider, SpikeVocabulary.Scope);

        Assert.That(kb.Ask(question).Judgment, Is.EqualTo(Judgment.Proved),
            "Re-publishing in the new epoch is the sanctioned repair, and it must restore the proof.");
    }

    [Test]
    public void Query_DepthBound_ReturnsUnknown()
    {
        // A three-rule chain against a depth budget of 2 proves an intermediate step but not the end.
        var kb = NewKb(maxDepth: 2);
        var provider = new SemanticProviderId("test.chain");
        var entity = new SemanticEntityId("x");

        kb.AddFact(new SemanticFact(
            new SemanticProposition(SpikeVocabulary.Chain0, entity),
            new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, provider)));

        kb.AddRule(new SemanticRule(provider,
            new SemanticProposition(SpikeVocabulary.Chain1, entity),
            [new SemanticProposition(SpikeVocabulary.Chain0, entity)]));
        kb.AddRule(new SemanticRule(provider,
            new SemanticProposition(SpikeVocabulary.Chain2, entity),
            [new SemanticProposition(SpikeVocabulary.Chain1, entity)]));
        kb.AddRule(new SemanticRule(provider,
            new SemanticProposition(SpikeVocabulary.Chain3, entity),
            [new SemanticProposition(SpikeVocabulary.Chain2, entity)]));

        var answer = kb.Ask(new SemanticProposition(SpikeVocabulary.Chain3, entity));

        Assert.Multiple(() =>
        {
            Assert.That(kb.Ask(new SemanticProposition(SpikeVocabulary.Chain1, entity)).Judgment, Is.EqualTo(Judgment.Proved));
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Unknown),
                "Exceeding the depth budget yields Unknown with a diagnostic; it never crashes and never becomes Disproved.");
            Assert.That(answer.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(SemanticDiagnosticCodes.DerivationDepthExceeded));
        });
    }

    [Test]
    public void Query_IsInconsistent_WhenTwoProvidersDisagreeOnOneTupleAtTheSameTrust()
    {
        var question = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);
        var kb = NewKb();

        kb.AddFact(new SemanticFact(question, new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, new SemanticProviderId("test.a"))));
        kb.AddFact(new SemanticFact(question, new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, new SemanticProviderId("test.b")), isNegative: true));

        var answer = kb.Ask(question);

        Assert.That(answer.Judgment, Is.EqualTo(Judgment.Inconsistent));
    }

    [Test]
    public void Query_NegativeRuleHead_DerivesDisprovedFromPositiveBody()
    {
        // The second sanctioned route to Disproved: a rule with a negative head whose body is positive.
        // This is not negation-as-failure — the body still has to be established.
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
        var provider = new SemanticProviderId("test.negative-rule");
        var entity = new SemanticEntityId("x");
        var target = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);

        kb.AddFact(new SemanticFact(
            SpikeVocabulary.Proposition(SpikeVocabulary.CapturesNothing, entity),
            new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, provider)));

        kb.AddRule(new SemanticRule(
            provider,
            target,
            [SpikeVocabulary.Proposition(SpikeVocabulary.CapturesNothing, entity)],
            isNegativeHead: true));

        var answer = kb.Ask(target);

        Assert.Multiple(() =>
        {
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Disproved));
            Assert.That(answer.Proof!.Origin, Is.EqualTo(EvidenceOrigin.DerivedByRule));
            Assert.That(answer.Proof.Premises, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Query_RuleIsNotSatisfiedByNegativeEvidence()
    {
        // No negation-as-failure in either direction: a negative fact cannot stand in for a positive
        // premise, so the rule simply does not fire and the head stays Unknown.
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
        var provider = new SemanticProviderId("test.no-naf");
        var entity = new SemanticEntityId("x");

        kb.AddFact(new SemanticFact(
            SpikeVocabulary.Proposition(SpikeVocabulary.CapturesNothing, entity),
            new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, provider),
            isNegative: true));

        kb.AddRule(new SemanticRule(
            provider,
            SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value),
            [SpikeVocabulary.Proposition(SpikeVocabulary.CapturesNothing, entity)]));

        Assert.That(kb.Ask(SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value)).Judgment,
            Is.EqualTo(Judgment.Unknown));
    }

    [Test]
    public void Query_TrustPrecedence_ResolvesDisagreementWithoutCallingItInconsistent()
    {
        var question = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);
        var kb = NewKb();

        kb.AddFact(new SemanticFact(
            question,
            new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, new SemanticProviderId("test.trusted")),
            trust: SemanticTrust.BuiltIn));

        kb.AddFact(new SemanticFact(
            question,
            new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, new SemanticProviderId("test.unchecked")),
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
}
