using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDslOrderingTests
{
    [Test]
    public void DirectiveParserOrder_ShouldCompareBySlotThenSequence()
    {
        var moduleSelection = new DialectDirectiveParserOrder(DialectDirectiveSlot.ModuleSelection, 1);
        var moduleOrdering = new DialectDirectiveParserOrder(DialectDirectiveSlot.ModuleOrdering, 0);
        var earlierWithinSlot = new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 1);
        var laterWithinSlot = new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 2);

        Assert.Multiple(() =>
        {
            Assert.That(moduleSelection.CompareTo(moduleOrdering), Is.LessThan(0));
            Assert.That(moduleOrdering.CompareTo(moduleSelection), Is.GreaterThan(0));
            Assert.That(earlierWithinSlot.CompareTo(laterWithinSlot), Is.LessThan(0));
            Assert.That(laterWithinSlot.CompareTo(earlierWithinSlot), Is.GreaterThan(0));
            Assert.That(earlierWithinSlot.CompareTo(new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 1)), Is.Zero);
        });
    }

    [Test]
    public void ParserOrder_ShouldCompareByStageThenSlotThenSequence()
    {
        var lineSplit = new DialectParserOrder(DialectParserStage.LineSplitting, 0, 0);
        var declaration = new DialectParserOrder(DialectParserStage.Declaration, 0, 0);
        var directiveEarly = new DialectParserOrder(DialectParserStage.Directives, 3, 0);
        var directiveLate = new DialectParserOrder(DialectParserStage.Directives, 3, 1);
        var document = new DialectParserOrder(DialectParserStage.Document, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(lineSplit.CompareTo(declaration), Is.LessThan(0));
            Assert.That(declaration.CompareTo(directiveEarly), Is.LessThan(0));
            Assert.That(directiveEarly.CompareTo(directiveLate), Is.LessThan(0));
            Assert.That(directiveLate.CompareTo(document), Is.LessThan(0));
            Assert.That(DialectParserOrder.Directive(new DialectDirectiveParserOrder(DialectDirectiveSlot.Security, 4)),
                Is.EqualTo(new DialectParserOrder(DialectParserStage.Directives, (int)DialectDirectiveSlot.Security, 4)));
        });
    }

    [Test]
    public void Registry_ShouldSortFeaturesDeterministically_ByParserOrderKeywordAndId()
    {
        var registry = new DialectDslRegistry(
            [
                new OrderedFeature("tests.zzz", "zzz", new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 5)),
                new OrderedFeature("tests.gamma", "gamma", new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 1)),
                new OrderedFeature("tests.alpha", "alpha", new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 0)),
                new OrderedFeature("tests.beta", "beta", new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 2))
            ],
            []);

        Assert.That(registry.DirectiveFeatures.Select(x => (x.Keyword, x.Id, x.ParserOrder.Sequence)), Is.EqualTo(new[]
        {
            ("alpha", "tests.alpha", 0),
            ("gamma", "tests.gamma", 1),
            ("beta", "tests.beta", 2),
            ("zzz", "tests.zzz", 5)
        }));
    }

    [Test]
    public void RegistryFactory_ShouldOrderProviderExecutionDeterministically_ByOrderThenTypeName()
    {
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        services.AddDialectDsl();
        services.AddSingleton(executionLog);
        services.AddSingleton<IDialectDslFeatureProvider>(provider => new RecordingProviderOmega(builder => builder.RegisterFeature(new OrderedFeature("tests.provider.omega", "omega", new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 2))), executionLog));
        services.AddSingleton<IDialectDslFeatureProvider>(provider => new RecordingProviderAlpha(builder => builder.RegisterFeature(new OrderedFeature("tests.provider.alpha", "alpha", new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 1))), executionLog));

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<DialectDslRegistry>();

        Assert.Multiple(() =>
        {
            Assert.That(executionLog, Is.EqualTo(new[] { nameof(RecordingProviderAlpha), nameof(RecordingProviderOmega) }));
            Assert.That(registry.DirectiveFeatures.Select(x => x.Keyword), Is.EqualTo(new[] { "alpha", "omega" }));
        });
    }

    [Test]
    public void ParserNodeRegistry_ShouldEmitContiguousPriorities_InOrderedStageSequence()
    {
        var registry = DialectDslTestComposition.CreateRegistry(services =>
        {
            services.AddDialectDirectiveFeature<DirectAliasDirectiveFeature>();
            services.AddDialectDirectiveFeature<SingletonNoteDirectiveFeature>();
        });

        var registrations = DialectDslParserNodeRegistry.CreateRegistrations(registry);

        Assert.Multiple(() =>
        {
            Assert.That(registrations.Select(x => x.Priority), Is.EqualTo(Enumerable.Range(0, registrations.Count).Select(static x => (float)x)));
            Assert.That(registrations.First().Creator, Is.TypeOf<DialectLineNodeCreator>());
            Assert.That(registrations[1].Creator, Is.TypeOf<DialectDeclarationNodeCreator>());
            Assert.That(registrations[^1].Creator, Is.TypeOf<DialectDocumentNodeCreator>());
            Assert.That(registrations.Skip(2).Take(registrations.Count - 3).Select(x => x.Creator.GetType().Name).Distinct(), Does.Contain(nameof(FeatureDialectDirectiveNodeCreator)));
        });
    }

    [Test]
    public void Registry_ShouldRejectDirectiveOrderCollisions_WithMeaningfulMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new DialectDslRegistry(
            [new CollisionFeatureA(), new CollisionFeatureB()],
            []));

        Assert.That(ex!.Message, Does.Contain("collision").And.Contain("tests.collision.a").And.Contain("tests.collision.b"));
    }

    [Test]
    public void Registry_ShouldRejectDuplicateKeywordAndDuplicateIdRegistrations()
    {
        var duplicateKeyword = Assert.Throws<InvalidOperationException>(() => new DialectDslRegistry(
            [new OrderedFeature("tests.alpha", "dup", new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 1)), new OrderedFeature("tests.beta", "dup", new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 2))],
            []));
        var duplicateId = Assert.Throws<InvalidOperationException>(() => new DialectDslRegistry(
            [new OrderedFeature("tests.same", "alpha", new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 1)), new OrderedFeature("tests.same", "beta", new DialectDirectiveParserOrder(DialectDirectiveSlot.Extension, 2))],
            []));

        Assert.Multiple(() =>
        {
            Assert.That(duplicateKeyword!.Message, Does.Contain("keyword").And.Contain("dup"));
            Assert.That(duplicateId!.Message, Does.Contain("identifier").And.Contain("tests.same"));
        });
    }

    private sealed class OrderedFeature(string id, string keyword, DialectDirectiveParserOrder parserOrder) : SimpleIdentifierDirectiveFeatureBase
    {
        public override string Id => id;
        public override string Keyword => keyword;
        public override DialectDirectiveParserOrder ParserOrder => parserOrder;
    }
}