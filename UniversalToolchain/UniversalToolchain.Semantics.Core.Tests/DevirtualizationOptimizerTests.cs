using System.Xml.Linq;
using UniversalToolchain.Semantics.Core;
using UniversalToolchain.Semantics.Core.TestFixtures;
using UniversalToolchain.Semantics.Core.TestFixtures.Provider;

namespace UniversalToolchain.Semantics.Core.Tests;

/// <summary>
/// UT-A030: an optimizer must be able to act on another module's semantics without importing it, and
/// must handle all four judgments rather than treating everything but <c>Proved</c> as "skip".
/// </summary>
[TestFixture]
public sealed class DevirtualizationOptimizerTests
{
    private static readonly SemanticEntityId Call = new("call#1");
    private static readonly SemanticEntityId Lambda = new("lambda#1");
    private static readonly SemanticEntityId OtherLambda = new("lambda#2");

    [Test]
    public void Optimizer_RewritesWhenExactTargetIsProved()
    {
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog())
            .AddProvider(new DelegateSemanticsProvider([(Call, Lambda)]), SpikeVocabulary.Scope);
        var optimizer = new DevirtualizationOptimizer(kb);

        var decision = optimizer.Decide(Call, Lambda);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Outcome, Is.EqualTo(OptimizationOutcome.Apply));
            Assert.That(decision.Rewrites, Is.True);
            Assert.That(decision.Answer.Judgment, Is.EqualTo(Judgment.Proved));
        });
    }

    [Test]
    public void Optimizer_SkipsWhenNothingIsKnown()
    {
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
        var optimizer = new DevirtualizationOptimizer(kb);

        var decision = optimizer.Decide(Call, Lambda);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Outcome, Is.EqualTo(OptimizationOutcome.Skipped));
            Assert.That(decision.Rewrites, Is.False);
            Assert.That(decision.Answer.Judgment, Is.EqualTo(Judgment.Unknown));
        });
    }

    [Test]
    public void Optimizer_SkipsWhenExactTargetIsDisproved()
    {
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
        var question = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);
        kb.AddFact(new SemanticFact(
            question,
            new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, DelegateSemanticsProvider.Provider),
            isNegative: true));

        var decision = new DevirtualizationOptimizer(kb).Decide(Call, Lambda);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Outcome, Is.EqualTo(OptimizationOutcome.Skipped));
            Assert.That(decision.Answer.Judgment, Is.EqualTo(Judgment.Disproved));
        });
    }

    [Test]
    public void Optimizer_RefusesRatherThanGuessesWhenProvidersDisagree()
    {
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
        var question = SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value);
        kb.AddFact(new SemanticFact(question, new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, new SemanticProviderId("test.a"))));
        kb.AddFact(new SemanticFact(question, new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, new SemanticProviderId("test.b")), isNegative: true));

        var decision = new DevirtualizationOptimizer(kb).Decide(Call, Lambda);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Outcome, Is.EqualTo(OptimizationOutcome.Refused),
                "Inconsistent is a defect signal; skipping it silently would hide a broken configuration.");
            Assert.That(decision.Rewrites, Is.False);
            Assert.That(decision.Answer.IsHardRefusal, Is.True);
        });
    }

    [Test]
    public void Optimizer_RefusesAfterEpochBump()
    {
        var provider = new DelegateSemanticsProvider([(Call, Lambda)]);
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog())
            .AddProvider(provider, SpikeVocabulary.Scope);

        Assert.That(new DevirtualizationOptimizer(kb).Decide(Call, Lambda).Rewrites, Is.True);

        kb.AdvanceEpoch();

        var stale = new DevirtualizationOptimizer(kb).Decide(Call, Lambda);
        Assert.Multiple(() =>
        {
            Assert.That(stale.Rewrites, Is.False);
            Assert.That(stale.Answer.Judgment, Is.EqualTo(Judgment.Unknown));
        });
    }

    [Test]
    public void Optimizer_AsksTheQuery_InsteadOfGuessing()
    {
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog())
            .AddProvider(new DelegateSemanticsProvider([(Call, Lambda)]), SpikeVocabulary.Scope);
        var recording = new RecordingQuery(kb);

        new DevirtualizationOptimizer(recording).Decide(Call, Lambda);

        Assert.That(recording.Asked, Has.Count.EqualTo(1));
        Assert.That(recording.Asked[0], Is.EqualTo(SpikeVocabulary.Proposition(SpikeVocabulary.ExactTarget, Call.Value, Lambda.Value)));
    }

    [Test]
    public void Optimizer_DerivesFromRulesWithoutSeeingTheOwningProvider()
    {
        // Derived evidence: the fact is implied by a rule rather than declared. The optimizer still only
        // sees the question, so derivation cannot leak provider identity into its decision.
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog());
        var receiver = new SemanticEntityId("receiver#7");
        var lambda = new SemanticEntityId("lambda#7");
        var call = new SemanticEntityId("call#7");

        kb.AddFact(new SemanticFact(
            new SemanticProposition(SpikeVocabulary.CapturesNothing, lambda),
            new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, DelegateSemanticsProvider.Provider)));

        kb.AddFact(new SemanticFact(
            new SemanticProposition(SpikeVocabulary.SameValue, receiver, lambda),
            new SemanticFactContext(SpikeVocabulary.Scope, kb.Epoch, DelegateSemanticsProvider.Provider)));

        kb.AddRuleProvider(new DelegateSemanticsProvider());

        var decision = new DevirtualizationOptimizer(kb).Decide(call, lambda);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Answer.Judgment, Is.EqualTo(Judgment.Proved));
            Assert.That(decision.Answer.Proof!.Origin, Is.EqualTo(EvidenceOrigin.DerivedByRule));
            Assert.That(decision.Rewrites, Is.True);
        });
    }

    [Test]
    public void Optimizer_DoesNotRewritesForAnUnrelatedTarget()
    {
        var kb = new SemanticKnowledgeBase(SpikeVocabulary.Catalog())
            .AddProvider(new DelegateSemanticsProvider([(Call, Lambda)]), SpikeVocabulary.Scope);

        var decision = new DevirtualizationOptimizer(kb).Decide(Call, OtherLambda);

        Assert.That(decision.Rewrites, Is.False,
            "Proving one pair says nothing about another; a devirtualizer must not generalize a single fact.");
    }

    [Test]
    public void OptimizerProject_DoesNotReferenceAnyFeatureProvider()
    {
        var root = FindRepositoryRoot();
        var project = Path.Combine(
            root,
            "UniversalToolchain",
            "UniversalToolchain.Semantics.Core.TestFixtures",
            "UniversalToolchain.Semantics.Core.TestFixtures.csproj");

        Assert.That(File.Exists(project), Is.True, $"fixture project is missing: {project}");

        var references = XDocument.Load(project)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(static include => !string.IsNullOrWhiteSpace(include))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(references, Has.Length.EqualTo(1),
                "The optimizer fixture must depend on the query surface alone; a second reference means it started importing a provider.");
            Assert.That(references[0]!.Replace('\\', '/'), Does.EndWith("UniversalToolchain.Semantics.Core/UniversalToolchain.Semantics.Core.csproj"));
            Assert.That(references[0]!, Does.Not.Contain("Provider"));
        });
    }

    [Test]
    public void SemanticCoreProject_DoesNotReferenceTheExecutionStack()
    {
        var root = FindRepositoryRoot();
        var project = Path.Combine(
            root,
            "UniversalToolchain",
            "UniversalToolchain.Semantics.Core",
            "UniversalToolchain.Semantics.Core.csproj");

        var references = XDocument.Load(project)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(static include => !string.IsNullOrWhiteSpace(include))
            .Select(static include => include!.Replace('\\', '/'))
            .ToArray();

        Assert.That(references, Has.Length.EqualTo(1), "ADR-003 pins the core to a single project reference.");
        Assert.That(references[0], Does.EndWith("UniversalToolchain.Language.Abstractions/UniversalToolchain.Language.Abstractions.csproj"));

        var forbidden = new[] { "BasicCore", "LanguageSdk", "Runtime", "ModuleContracts", "Wist" };
        Assert.That(references.Where(reference => forbidden.Any(token => reference.Contains(token, StringComparison.Ordinal))),
            Is.Empty,
            "The core must not inherit the .NET execution stack or the planner's authority.");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalToolchain", "Wistc", "Wistc.csproj")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate the repository root from '{TestContext.CurrentContext.TestDirectory}'.");
    }
}
