using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using UniversalToolchain.Intrinsics.Core.Rules;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class CoreIntrinsicDescriptorProviderTests
{
    private static readonly IntrinsicTypeResolutionContext Context = new();

    [Test]
    public void GetDescriptors_ShouldContainCoreBuiltInSymbols()
    {
        var provider = CreateProvider();

        var symbols = provider.GetDescriptors().Select(x => x.Symbol);

        Assert.That(symbols, Is.EqualTo(new[]
        {
            BuiltinIntrinsicSymbols.Core.CallCSharp,
            BuiltinIntrinsicSymbols.Core.CallCSharpCtor,
            BuiltinIntrinsicSymbols.Core.LoadExternal,
            BuiltinIntrinsicSymbols.Core.StoreExternal,
            BuiltinIntrinsicSymbols.Core.LoadConst
        }));
    }

    [Test]
    public void LoadConstDescriptor_ShouldUsePushSingleTypeRule()
    {
        var provider = CreateProvider();
        var descriptor = provider.GetDescriptors().Single(x => x.Symbol == BuiltinIntrinsicSymbols.Core.LoadConst);
        var invocation = CreateInvocation(BuiltinIntrinsicSymbols.Core.LoadConst, [IntrinsicTypeArgument.From(typeof(decimal))]);
        var stack = new List<Type>();

        Assert.That(descriptor.StackRule, Is.TypeOf<PushSingleTypeRule>());

        descriptor.StackRule.Apply(invocation, stack, Context);

        Assert.That(stack, Is.EqualTo(new[] { typeof(decimal) }));
    }

    [Test]
    public void CallCSharpDescriptor_ShouldExist()
    {
        var provider = CreateProvider();

        var descriptor = provider.GetDescriptors().SingleOrDefault(x => x.Symbol == BuiltinIntrinsicSymbols.Core.CallCSharp);

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Category, Is.EqualTo(IntrinsicCategory.Interop));
        Assert.That(descriptor.StackRule, Is.TypeOf<CSharpCallStackRule>());
    }

    [Test]
    public void LoadExternalDescriptor_ShouldRequireOneTypeArgument()
    {
        var provider = CreateProvider();
        var descriptor = provider.GetDescriptors().Single(x => x.Symbol == BuiltinIntrinsicSymbols.Core.LoadExternal);
        var invocation = CreateInvocation(BuiltinIntrinsicSymbols.Core.LoadExternal);

        var exception = Assert.Throws<InvalidOperationException>(() => descriptor.ValidationRule.Validate(invocation, Context));

        Assert.That(exception!.Message, Does.Contain("Expected 1 type arguments"));
    }

    private static CoreIntrinsicDescriptorProvider CreateProvider()
    {
        return new CoreIntrinsicDescriptorProvider(new MethodCallTypeSemanticsResolver());
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
