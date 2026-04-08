using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Capabilities;
using UniversalToolchain.Intrinsics.Contracts;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class IntrinsicCapabilityServiceCollectionTests
{
    [Test]
    public void AddCoreRuntimeInfrastructure_ShouldRegisterCapabilitySetFactory()
    {
        var services = new ServiceCollection();
        services.AddCoreRuntimeInfrastructure();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IIntrinsicCapabilitySetFactory>();
        var capabilitySet = factory.Create(new FakeCompiler(["add_i32"]));

        Assert.That(factory, Is.TypeOf<CompilerIntrinsicCapabilitySetFactory>());
        Assert.That(
            capabilitySet.Supports(
                BuiltinIntrinsicSymbols.Arithmetic.Add,
                [IntrinsicTypeArgument.From(typeof(int))]),
            Is.True);
    }

    private sealed class FakeCompiler(IReadOnlyList<string> supportedIntrinsics) : IAbstractIrCompiler<object>
    {
        public IReadOnlyList<string> SupportedIntrinsics { get; } = supportedIntrinsics;

        public object Compile(IAbstractIR air, CompilationInput input)
        {
            throw new NotSupportedException();
        }
    }
}
