using SettableGettableModule.Core;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using UniversalToolchain.Intrinsics.Core.Rules;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class IntrinsicStackRulesTests
{
    private static readonly IntrinsicTypeResolutionContext Context = new();
    private static readonly IntrinsicInvocation EmptyInvocation = CreateInvocation();

    [Test]
    public void BinarySameTypeResultRule_ShouldPushSameType()
    {
        var rule = new BinarySameTypeResultRule();
        var stack = new List<Type> { typeof(int), typeof(int) };
        var invocation = CreateInvocation(typeArguments: [IntrinsicTypeArgument.From(typeof(int))]);

        rule.Apply(invocation, stack, Context);

        Assert.That(stack, Is.EqualTo(new[] { typeof(int) }));
    }

    [Test]
    public void BinaryComparisonRule_ShouldPushBool()
    {
        var rule = new BinaryComparisonRule();
        var stack = new List<Type> { typeof(int), typeof(int) };
        var invocation = CreateInvocation(typeArguments: [IntrinsicTypeArgument.From(typeof(int))]);

        rule.Apply(invocation, stack, Context);

        Assert.That(stack, Is.EqualTo(new[] { typeof(bool) }));
    }

    [Test]
    public void BooleanUnaryRule_ShouldRequireBool()
    {
        var rule = new BooleanUnaryRule();
        var stack = new List<Type> { typeof(int) };

        Assert.Throws<InvalidOperationException>(() => rule.Apply(EmptyInvocation, stack, Context));
    }

    [Test]
    public void BooleanBinaryRule_ShouldRequireTwoBools()
    {
        var rule = new BooleanBinaryRule();
        var stack = new List<Type> { typeof(bool), typeof(int) };

        Assert.Throws<InvalidOperationException>(() => rule.Apply(EmptyInvocation, stack, Context));
    }

    [Test]
    public void PushSingleTypeRule_ShouldPushResolvedType()
    {
        var rule = new PushSingleTypeRule((_, _) => typeof(string));
        var stack = new List<Type>();

        rule.Apply(EmptyInvocation, stack, Context);

        Assert.That(stack, Is.EqualTo(new[] { typeof(string) }));
    }

    [Test]
    public void LoadLocalRefStackRule_ShouldPushVariableReferenceOfResolvedType()
    {
        var rule = new LoadLocalRefStackRule();
        var stack = new List<Type>();
        var invocation = CreateInvocation(typeArguments: [IntrinsicTypeArgument.From(typeof(int))]);

        rule.Apply(invocation, stack, Context);

        Assert.That(stack, Is.EqualTo(new[] { typeof(VariableReference<int>) }));
    }

    private static IntrinsicInvocation CreateInvocation(
        IReadOnlyList<IntrinsicTypeArgument>? typeArguments = null,
        IReadOnlyList<object?>? dataOperands = null)
    {
        return new IntrinsicInvocation(
            new IntrinsicSymbol("test", "intrinsic"),
            typeArguments ?? [],
            dataOperands ?? []);
    }
}
