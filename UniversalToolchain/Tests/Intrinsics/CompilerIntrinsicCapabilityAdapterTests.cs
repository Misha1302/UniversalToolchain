using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Capabilities;
using UniversalToolchain.Intrinsics.Contracts;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class CompilerIntrinsicCapabilityAdapterTests
{
    [Test]
    public void Supports_ShouldReturnTrue_ForArithmeticIntrinsicWithKnownSuffix()
    {
        var compiler = new FakeCompiler(["add_i32"]);
        var adapter = new CompilerIntrinsicCapabilityAdapter<object>(compiler);

        var isSupported = adapter.Supports(
            BuiltinIntrinsicSymbols.Arithmetic.Add,
            [IntrinsicTypeArgument.From(typeof(int))]);

        Assert.That(isSupported, Is.True);
    }

    [Test]
    public void Supports_ShouldReturnTrue_ForBooleanIntrinsic()
    {
        var compiler = new FakeCompiler(["boolean_not"]);
        var adapter = new CompilerIntrinsicCapabilityAdapter<object>(compiler);

        var isSupported = adapter.Supports(
            BuiltinIntrinsicSymbols.Boolean.Not,
            []);

        Assert.That(isSupported, Is.True);
    }

    [Test]
    public void Supports_ShouldReturnFalse_ForUnsupportedIntrinsic()
    {
        var compiler = new FakeCompiler(["boolean_not"]);
        var adapter = new CompilerIntrinsicCapabilityAdapter<object>(compiler);

        var isSupported = adapter.Supports(
            BuiltinIntrinsicSymbols.Arithmetic.Add,
            [IntrinsicTypeArgument.From(typeof(int))]);

        Assert.That(isSupported, Is.False);
    }

    [Test]
    public void Factory_Create_ShouldReturnCapabilityAdapterBoundToCompiler()
    {
        var factory = new CompilerIntrinsicCapabilitySetFactory();
        var compiler = new FakeCompiler(["add_i32"]);

        var capabilitySet = factory.Create(compiler);
        var isSupported = capabilitySet.Supports(
            BuiltinIntrinsicSymbols.Arithmetic.Add,
            [IntrinsicTypeArgument.From(typeof(int))]);

        Assert.That(capabilitySet, Is.TypeOf<CompilerIntrinsicCapabilityAdapter<object>>());
        Assert.That(isSupported, Is.True);
    }

    private sealed class FakeCompiler(IReadOnlyList<string> supportedIntrinsics) : IAbstractIrCompiler<object>
    {
        public IReadOnlyList<string> SupportedIntrinsics { get; } = supportedIntrinsics;

        public object Compile(IAbstractIR air, CompilationInput input) => throw new NotSupportedException();
    }
}