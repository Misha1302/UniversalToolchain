using UniversalToolchain.Semantics.Core;
using UniversalToolchain.Semantics.Core.TestFixtures.Provider;

namespace UniversalToolchain.Semantics.Core.Tests;

/// <summary>
/// The properties that make a semantic answer safe to act on: determinism, monotonicity, and the
/// explicit boundaries of what the core is allowed to do.
/// </summary>
[TestFixture]
public sealed class SemanticCoreInvariantTests
{
    private static readonly SemanticEntityId Call = new("call#1");
    private static readonly SemanticEntityId Lambda = new("lambda#1");

    private static readonly SemanticProposition Question =
        SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);

    private static SemanticFact Fact(
        SemanticProposition proposition,
        CompilationEpoch epoch,
        string provider,
        bool isNegative = false,
        SemanticTrust trust = SemanticTrust.Verified) =>
        new(proposition, new SemanticFactContext(SpikeVocabulary.Scope, epoch, new SemanticProviderId(provider)), isNegative, trust: trust);

    [Test]
    public void Query_Fixpoint_DAGIdenticalUnderShuffledAxioms()
    {
        // Same knowledge, different registration order. The proof text must be byte-identical, because a
        // golden hash over evidence is only meaningful if order cannot leak into it.
        var chainEntity = new SemanticEntityId("x");
        var provider = new SemanticProviderId("test.chain");

        SemanticFact[] facts =
        [
            Fact(new SemanticProposition(SpikeVocabulary.Chain0, chainEntity), new CompilationEpoch(0), provider.Value),
            Fact(new SemanticProposition(SpikeVocabulary.Chain1, chainEntity), new CompilationEpoch(0), provider.Value),
            Fact(new SemanticProposition(SpikeVocabulary.Chain2, chainEntity), new CompilationEpoch(0), provider.Value),
            Fact(new SemanticProposition(SpikeVocabulary.ExactTarget, Call, Lambda), new CompilationEpoch(0), "test.a")
        ];

        SemanticRule[] rules =
        [
            new(provider, new SemanticProposition(SpikeVocabulary.Chain3, chainEntity), [new SemanticProposition(SpikeVocabulary.Chain2, chainEntity)]),
            new(provider, new SemanticProposition(SpikeVocabulary.Chain2, chainEntity), [new SemanticProposition(SpikeVocabulary.Chain1, chainEntity)]),
            new(provider, new SemanticProposition(SpikeVocabulary.Chain1, chainEntity), [new SemanticProposition(SpikeVocabulary.Chain0, chainEntity)])
        ];

        var renders = new List<string>();
        var chainQuestion = new SemanticProposition(SpikeVocabulary.Chain3, chainEntity);

        foreach (var permutation in Permutations(facts.Length))
        {
            var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
            foreach (var index in permutation)
                kb.AddFact(facts[index]);
            foreach (var rule in rules.Reverse())
                kb.AddRule(rule);

            var answer = kb.Ask(chainQuestion);
            Assert.That(answer.Judgment, Is.EqualTo(Judgment.Proved));
            renders.Add(answer.Proof!.Render());
        }

        Assert.That(renders.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1),
            "Registration order must not change the evidence DAG.");
    }

    [Test]
    public void Query_Monotonicity_AddingFactsNeverDowngradesProvedToUnknown()
    {
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());

        Assert.That(kb.Ask(Question).Judgment, Is.EqualTo(Judgment.Unknown));

        kb.AddFact(Fact(Question, kb.Epoch, "test.a"));
        Assert.That(kb.Ask(Question).Judgment, Is.EqualTo(Judgment.Proved));

        // An unrelated fact must not disturb an established proof.
        kb.AddFact(Fact(
            SpikeVocabulary.Proposition(SpikeVocabulary.CapturesNothing, "other"),
            kb.Epoch,
            "test.b"));

        Assert.That(kb.Ask(Question).Judgment, Is.EqualTo(Judgment.Proved));
    }

    [Test]
    public void Query_DistinctFromInconsistent_ProvedAndInconsistentAreDistinguishable()
    {
        // Red-team R-2: the spike is only worth continuing if these two states are genuinely different
        // outcomes, not two names for "not proved".
        var single = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
        single.AddFact(Fact(Question, single.Epoch, "test.a"));

        var clash = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
        clash.AddFact(Fact(Question, clash.Epoch, "test.a"));
        clash.AddFact(Fact(Question, clash.Epoch, "test.b", isNegative: true));

        var proved = single.Ask(Question);
        var inconsistent = clash.Ask(Question);

        Assert.Multiple(() =>
        {
            Assert.That(proved.Judgment, Is.EqualTo(Judgment.Proved));
            Assert.That(inconsistent.Judgment, Is.EqualTo(Judgment.Inconsistent));
            Assert.That(proved.Judgment, Is.Not.EqualTo(inconsistent.Judgment));
            Assert.That(proved.Proof, Is.Not.Null);
            Assert.That(inconsistent.Proof, Is.Null, "A contradiction has no single witness to hand to a consumer.");
        });
    }

    [Test]
    public void Query_Memoization_DoesNotLeakAcrossEpochs()
    {
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
        kb.AddFact(Fact(Question, kb.Epoch, "test.a"));

        var first = kb.Ask(Question);
        var second = kb.Ask(Question);

        Assert.That(second, Is.SameAs(first), "Repeated queries in one epoch should hit the memo.");

        kb.AdvanceEpoch();
        var third = kb.Ask(Question);

        Assert.Multiple(() =>
        {
            Assert.That(third, Is.Not.SameAs(first));
            Assert.That(third.Judgment, Is.EqualTo(Judgment.Unknown));
        });
    }

    [Test]
    public void Provider_CannotPublishFactsOwnedByAnotherProvider()
    {
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
        var impostor = new ImpostorProvider();

        var thrown = Assert.Throws<InvalidOperationException>(() => kb.AddProvider(impostor, SpikeVocabulary.Scope));

        Assert.That(thrown!.Message, Does.Contain("test.delegates"));
    }

    /// <summary>
    /// Retained guard: the semantic core owns questions, not contributions. If any type here ever grows a
    /// selection-shaped member, this test is where the review discussion has to happen.
    /// </summary>
    [Test]
    public void Core_ExposesNoSelectionSurface()
    {
        var forbiddenMemberNames = new[]
        {
            "Select", "SelectContributions", "ResolveRoute", "Order",
            "AddContribution", "RemoveContribution", "Plan"
        };

        var offenders = typeof(SemanticKnowledgeBase).Assembly
            .GetExportedTypes()
            .SelectMany(type => type.GetMembers()
                .Where(member => member.DeclaringType == type)
                .Select(member => (Type: type, Member: member.Name)))
            .Where(entry => forbiddenMemberNames.Contains(entry.Member, StringComparer.Ordinal))
            .Select(entry => $"{entry.Type.Name}.{entry.Member}")
            .ToArray();

        Assert.That(offenders, Is.Empty,
            "The semantic core must answer questions only; contribution selection belongs to the single planner.");
    }

    private static IEnumerable<int[]> Permutations(int length)
    {
        // Only a handful of axioms are permuted, so a full enumeration is cheap and deterministic.
        if (length == 0)
        {
            yield return [];
            yield break;
        }

        if (length > 6)
            throw new ArgumentOutOfRangeException(nameof(length), "Test helper only permutes small sets.");

        var indexes = Enumerable.Range(0, length).ToArray();
        foreach (var permutation in Enumerate(indexes, 0))
            yield return permutation;
    }

    private static IEnumerable<int[]> Enumerate(int[] values, int start)
    {
        if (start == values.Length)
        {
            yield return (int[])values.Clone();
            yield break;
        }

        for (var index = start; index < values.Length; index++)
        {
            (values[start], values[index]) = (values[index], values[start]);
            foreach (var permutation in Enumerate(values, start + 1))
                yield return permutation;
            (values[start], values[index]) = (values[index], values[start]);
        }
    }

    private sealed class ImpostorProvider : ISemanticFactProvider
    {
        public SemanticProviderId Id => new("test.impostor");

        public IReadOnlyCollection<SemanticFact> DeclarePrimitiveFacts(SemanticFactContext context) =>
        [
            new SemanticFact(
                SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value),
                new SemanticFactContext(context.Scope, context.Epoch, DelegateSemanticsProvider.Provider))
        ];
    }
}
