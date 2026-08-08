using CommonExceptions;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDslTypedStateTests
{
    [Test]
    public void Accumulation_ShouldReuseLogicalListKey_ForSameNameAndType()
    {
        var accumulation = new DialectDirectiveAccumulation();
        var firstKey = new DialectListStateKey<string>("tests.list");
        var secondKey = new DialectListStateKey<string>("tests.list");

        accumulation.GetOrCreateList(firstKey).Add("alpha");

        Assert.That(accumulation.GetOrCreateList(secondKey), Is.EqualTo(new[] { "alpha" }));
    }

    [Test]
    public void Accumulation_ShouldIsolateListsAcrossDifferentTypes_EvenWhenNamesMatch()
    {
        var accumulation = new DialectDirectiveAccumulation();
        var stringKey = new DialectListStateKey<string>("tests.shared");
        var intKey = new DialectListStateKey<int>("tests.shared");

        accumulation.GetOrCreateList(stringKey).Add("alpha");
        accumulation.GetOrCreateList(intKey).Add(42);

        Assert.Multiple(() =>
        {
            Assert.That(accumulation.GetOrCreateList(stringKey), Is.EqualTo(new[] { "alpha" }));
            Assert.That(accumulation.GetOrCreateList(intKey), Is.EqualTo(new[] { 42 }));
        });
    }

    [Test]
    public void Accumulation_ShouldStoreAndRetrieveValues_ByTypedKey()
    {
        var accumulation = new DialectDirectiveAccumulation();
        var boolKey = new DialectValueStateKey<bool?>("tests.flag");
        var stringKey = new DialectValueStateKey<string>("tests.name");

        accumulation.SetValue(boolKey, true);
        accumulation.SetValue(stringKey, "dialect");

        Assert.Multiple(() =>
        {
            Assert.That(accumulation.GetValue(boolKey), Is.True);
            Assert.That(accumulation.GetValue(stringKey), Is.EqualTo("dialect"));
            Assert.That(accumulation.GetValue(new DialectValueStateKey<int?>("tests.missing")), Is.Null);
        });
    }

    [Test]
    public void Accumulation_ShouldRejectDuplicateSingletonValue_ForSameLogicalKey()
    {
        var accumulation = new DialectDirectiveAccumulation();
        var key = new DialectValueStateKey<int?>("tests.singleton");

        accumulation.SetSingletonValue(key, 1, "duplicate singleton");
        var ex = Assert.Throws<ParserException>(() => accumulation.SetSingletonValue(new DialectValueStateKey<int?>("tests.singleton"), 2, "duplicate singleton"));

        DialectDslTestSupport.AssertParserExceptionContains(ex!, "duplicate singleton");
    }

    [Test]
    public void ValidationContext_ShouldReuseStateObject_ForSameLogicalValueKey()
    {
        var context = new DialectDirectiveValidationContext();
        var firstKey = new DialectValueStateKey<List<string>>("tests.state");
        var secondKey = new DialectValueStateKey<List<string>>("tests.state");

        var state = context.GetOrAddState(firstKey, static () => []);
        state.Add("alpha");

        Assert.That(context.GetOrAddState(secondKey, static () => throw new InvalidOperationException()), Is.SameAs(state));
    }

    [Test]
    public void ValidationContext_ShouldIsolateStatesAcrossDifferentTypes_EvenWhenNamesMatch()
    {
        var context = new DialectDirectiveValidationContext();
        var listKey = new DialectValueStateKey<List<string>>("tests.state");
        var setKey = new DialectSetStateKey<string>("tests.state", StringComparer.OrdinalIgnoreCase);

        context.GetOrAddState(listKey, static () => []).Add("alpha");
        context.AddValue(setKey, "VALUE", "duplicate", null);

        Assert.Multiple(() =>
        {
            Assert.That(context.GetOrAddState(listKey, static () => throw new InvalidOperationException()), Is.EqualTo(new[] { "alpha" }));
            Assert.That(context.GetValues(setKey), Is.EquivalentTo(new[] { "VALUE" }));
        });
    }

    [Test]
    public void ValidationContext_ShouldHonorSetComparer_WhenSameLogicalKeyIsReused()
    {
        var context = new DialectDirectiveValidationContext();
        var firstKey = new DialectSetStateKey<string>("tests.case-insensitive", StringComparer.OrdinalIgnoreCase);
        var secondKey = new DialectSetStateKey<string>("tests.case-insensitive", StringComparer.OrdinalIgnoreCase);

        context.AddValue(firstKey, "VALUE", "duplicate", null);
        var ex = Assert.Throws<ParserException>(() => context.AddValue(secondKey, "value", "duplicate", null));

        DialectDslTestSupport.AssertParserExceptionContains(ex!, "duplicate");
    }

    [Test]
    public void ValidationContext_ShouldRejectNullFactoryResults_ForTypedState()
    {
        var context = new DialectDirectiveValidationContext();
        var key = new DialectValueStateKey<List<string>>("tests.null-state");

        var ex = Assert.Throws<InvalidOperationException>(() => context.GetOrAddState(key, static () => null!));

        Assert.That(ex!.Message, Does.Contain("returned null"));
    }

    [Test]
    public void TypedStateKeys_ShouldRejectNullOrWhitespaceNames()
    {
        var accumulation = new DialectDirectiveAccumulation();
        var validationContext = new DialectDirectiveValidationContext();

        var accumulationException = Assert.Throws<ArgumentException>(() =>
            accumulation.GetOrCreateList(new DialectListStateKey<string>(" ")));
        var validationException = Assert.Throws<ArgumentException>(() =>
            validationContext.GetOrAddState(new DialectValueStateKey<List<string>>(string.Empty), static () => []));

        Assert.Multiple(() =>
        {
            Assert.That(accumulationException!.Message, Does.Contain("must not be empty"));
            Assert.That(validationException!.Message, Does.Contain("must not be empty"));
        });
    }

    [Test]
    public void BuiltInValidationState_ShouldNotLeakAcrossIntrinsicAndOptimizerNamespaces_WhenNamesMatch()
    {
        var slice = DialectDslTestComposition.CreateCompiler().Compile(
            "dialect Demo\nallow shared\nenable shared\nforbid blocked\ndisable blocked\n");

        Assert.Multiple(() =>
        {
            Assert.That(slice.IntrinsicDirectives.Select(x => (x.Name, x.Allowed)), Is.EqualTo(new[] { ("shared", true), ("blocked", false) }));
            Assert.That(slice.OptimizerDirectives.Select(x => (x.Name, x.Enabled)), Is.EqualTo(new[] { ("shared", true), ("blocked", false) }));
        });
    }
}
