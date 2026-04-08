using SettableGettableModule.Core;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using UniversalToolchain.Intrinsics.Core.Rules;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class BuiltInDescriptorProvidersTests
{
    private static readonly IntrinsicTypeResolutionContext Context = new();

    [Test]
    public void ArithmeticProvider_ShouldExposeFourDescriptors()
    {
        var provider = new ArithmeticIntrinsicDescriptorProvider();

        var symbols = provider.GetDescriptors().Select(descriptor => descriptor.Symbol);

        Assert.That(symbols, Is.EqualTo(new[]
        {
            BuiltinIntrinsicSymbols.Arithmetic.Add,
            BuiltinIntrinsicSymbols.Arithmetic.Subtract,
            BuiltinIntrinsicSymbols.Arithmetic.Multiply,
            BuiltinIntrinsicSymbols.Arithmetic.Divide
        }));
    }

    [Test]
    public void ComparisonProvider_ShouldExposeSixDescriptors()
    {
        var provider = new ComparisonIntrinsicDescriptorProvider();

        var symbols = provider.GetDescriptors().Select(descriptor => descriptor.Symbol);

        Assert.That(symbols, Is.EqualTo(new[]
        {
            BuiltinIntrinsicSymbols.Comparison.Equal,
            BuiltinIntrinsicSymbols.Comparison.NotEqual,
            BuiltinIntrinsicSymbols.Comparison.Greater,
            BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual,
            BuiltinIntrinsicSymbols.Comparison.Less,
            BuiltinIntrinsicSymbols.Comparison.LessOrEqual
        }));
    }

    [Test]
    public void BooleanProvider_ShouldExposeThreeDescriptors()
    {
        var provider = new BooleanIntrinsicDescriptorProvider();

        var symbols = provider.GetDescriptors().Select(descriptor => descriptor.Symbol);

        Assert.That(symbols, Is.EqualTo(new[]
        {
            BuiltinIntrinsicSymbols.Boolean.And,
            BuiltinIntrinsicSymbols.Boolean.Or,
            BuiltinIntrinsicSymbols.Boolean.Not
        }));
    }

    [Test]
    public void StorageProvider_ShouldExposeThreeDescriptors()
    {
        var provider = new StorageIntrinsicDescriptorProvider();

        var symbols = provider.GetDescriptors().Select(descriptor => descriptor.Symbol);

        Assert.That(symbols, Is.EqualTo(new[]
        {
            BuiltinIntrinsicSymbols.Storage.LoadLocal,
            BuiltinIntrinsicSymbols.Storage.StoreLocal,
            BuiltinIntrinsicSymbols.Storage.LoadLocalRef
        }));
    }

    [Test]
    public void ArithmeticDescriptors_ShouldUseBinarySameTypeResultRule()
    {
        var provider = new ArithmeticIntrinsicDescriptorProvider();
        var descriptors = provider.GetDescriptors();
        var invocation = CreateInvocation(
            BuiltinIntrinsicSymbols.Arithmetic.Add,
            [IntrinsicTypeArgument.From(typeof(decimal))]);

        foreach (var descriptor in descriptors)
        {
            var stack = new List<Type> { typeof(decimal), typeof(decimal) };

            Assert.That(descriptor.Category, Is.EqualTo(IntrinsicCategory.Arithmetic));
            Assert.That(descriptor.StackRule, Is.TypeOf<BinarySameTypeResultRule>());

            descriptor.ValidationRule.Validate(invocation, Context);
            descriptor.StackRule.Apply(invocation, stack, Context);

            Assert.That(stack, Is.EqualTo(new[] { typeof(decimal) }));
        }
    }

    [Test]
    public void ComparisonDescriptors_ShouldUseBinaryComparisonRule()
    {
        var provider = new ComparisonIntrinsicDescriptorProvider();
        var descriptors = provider.GetDescriptors();
        var invocation = CreateInvocation(
            BuiltinIntrinsicSymbols.Comparison.Equal,
            [IntrinsicTypeArgument.From(typeof(int))]);

        foreach (var descriptor in descriptors)
        {
            var stack = new List<Type> { typeof(int), typeof(int) };

            Assert.That(descriptor.Category, Is.EqualTo(IntrinsicCategory.Comparison));
            Assert.That(descriptor.StackRule, Is.TypeOf<BinaryComparisonRule>());

            descriptor.ValidationRule.Validate(invocation, Context);
            descriptor.StackRule.Apply(invocation, stack, Context);

            Assert.That(stack, Is.EqualTo(new[] { typeof(bool) }));
        }
    }

    [Test]
    public void StorageLoadLocalRef_ShouldUseLoadLocalRefStackRule()
    {
        var provider = new StorageIntrinsicDescriptorProvider();
        var descriptor = provider.GetDescriptors().Single(item => item.Symbol == BuiltinIntrinsicSymbols.Storage.LoadLocalRef);
        var invocation = CreateInvocation(
            BuiltinIntrinsicSymbols.Storage.LoadLocalRef,
            [IntrinsicTypeArgument.From(typeof(int))]);
        var stack = new List<Type>();

        Assert.That(descriptor.Category, Is.EqualTo(IntrinsicCategory.Storage));
        Assert.That(descriptor.StackRule, Is.TypeOf<LoadLocalRefStackRule>());

        descriptor.ValidationRule.Validate(invocation, Context);
        descriptor.StackRule.Apply(invocation, stack, Context);

        Assert.That(stack, Is.EqualTo(new[] { typeof(VariableReference<int>) }));
    }

    private static IntrinsicInvocation CreateInvocation(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument>? typeArguments = null,
        IReadOnlyList<object?>? dataOperands = null)
    {
        return new IntrinsicInvocation(
            symbol,
            typeArguments ?? [],
            dataOperands ?? []);
    }
}
