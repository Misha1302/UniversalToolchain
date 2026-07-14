using BasicCore.Core.Rules;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class IntrinsicStackRulesTests
{
    private static readonly IntrinsicTypeResolutionContext _context = new();
    private static readonly IntrinsicInvocation _emptyInvocation = CreateInvocation();

    [Test]
    public void BinarySameTypeResultRule_ShouldPushSameType()
    {
        var rule = new BinarySameTypeResultRule();
        var stack = new List<Type> { typeof(int), typeof(int) };
        var invocation = CreateInvocation([IntrinsicTypeArgument.From(typeof(int))]);

        rule.Apply(invocation, stack, _context);

        Assert.That(stack, Is.EqualTo(new[] { typeof(int) }));
    }

    [Test]
    public void BinaryComparisonRule_ShouldPushBool()
    {
        var rule = new BinaryComparisonRule();
        var stack = new List<Type> { typeof(int), typeof(int) };
        var invocation = CreateInvocation([IntrinsicTypeArgument.From(typeof(int))]);

        rule.Apply(invocation, stack, _context);

        Assert.That(stack, Is.EqualTo(new[] { typeof(bool) }));
    }

    [Test]
    public void BooleanUnaryRule_ShouldRequireBool()
    {
        var rule = new BooleanUnaryRule();
        var stack = new List<Type> { typeof(int) };

        Assert.Throws<InvalidOperationException>(() => rule.Apply(_emptyInvocation, stack, _context));
    }

    [Test]
    public void BooleanBinaryRule_ShouldRequireTwoBools()
    {
        var rule = new BooleanBinaryRule();
        var stack = new List<Type> { typeof(bool), typeof(int) };

        Assert.Throws<InvalidOperationException>(() => rule.Apply(_emptyInvocation, stack, _context));
    }

    [Test]
    public void PushSingleTypeRule_ShouldPushResolvedType()
    {
        var rule = new PushSingleTypeRule((_, _) => typeof(string));
        var stack = new List<Type>();

        rule.Apply(_emptyInvocation, stack, _context);

        Assert.That(stack, Is.EqualTo(new[] { typeof(string) }));
    }

    [Test]
    public void LoadLocalRefStackRule_ShouldPushManagedByRefType()
    {
        var rule = new LoadLocalRefStackRule();
        var stack = new List<Type>();
        var invocation = CreateInvocation([IntrinsicTypeArgument.From(typeof(int))]);

        rule.Apply(invocation, stack, _context);

        Assert.That(stack, Is.EqualTo(new[] { typeof(int).MakeByRefType() }));
    }

    private static IntrinsicInvocation CreateInvocation(
        IReadOnlyList<IntrinsicTypeArgument>? typeArguments = null,
        IReadOnlyList<object?>? dataOperands = null) =>
        new(
            new IntrinsicSymbol("test", "intrinsic"),
            typeArguments ?? [],
            dataOperands ?? []);
}