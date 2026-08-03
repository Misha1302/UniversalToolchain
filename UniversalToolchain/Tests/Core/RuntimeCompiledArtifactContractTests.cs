using DynamicMethodCalling;
using Tests.Infrastructure;

namespace Tests.Core;

[TestFixture]
public class RuntimeCompiledArtifactContractTests
{
    [Test]
    public void Compile_WithDeclaredBindings_ShouldPreserveBindingOrderInSlotsByName()
    {
        using var host = RuntimeCompiledArtifactTestFactory.CreateHost();
        var declared = new OrderedDictionary<string, Type>
        {
            ["beta"] = typeof(object),
            ["alpha"] = typeof(object),
            ["gamma"] = typeof(object)
        };

        var artifact = host.Compile("alpha", declared, "cil");

        Assert.That(artifact.DeclaredBindings.Select(static b => b.Name), Is.EqualTo(new[] { "beta", "alpha", "gamma" }));
        Assert.That(artifact.SlotsByName["beta"], Is.EqualTo(0));
        Assert.That(artifact.SlotsByName["alpha"], Is.EqualTo(1));
        Assert.That(artifact.SlotsByName["gamma"], Is.EqualTo(2));
    }

    [Test]
    public void DynamicMethodArtifact_AsFunc_SingleArgument_ReturnsExpectedValue()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateUnaryAddOneArtifact();

        var result = artifact.AsFunc<int, int>()(41);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void DynamicMethodArtifact_AsFunc_TwoArguments_ReturnsExpectedValue()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateBinaryArtifact();

        var result = artifact.AsFunc<int, int, int>()(6, 7);

        Assert.That(result, Is.EqualTo(67));
    }

    [Test]
    public void DynamicMethodArtifact_NativeInvoker_InvokeSingleArgument_ReturnsExpectedValue()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateUnaryAddOneArtifact();

        var result = artifact.GetNativeDelegateInvoker().Invoke<int, int>(9);

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void DynamicMethodArtifact_NativeInvoker_InvokeTwoArguments_ReturnsExpectedValue()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateBinaryArtifact();

        var result = artifact.GetNativeDelegateInvoker().Invoke<int, int, int>(4, 5);

        Assert.That(result, Is.EqualTo(45));
    }

    [Test]
    public void DynamicMethodArtifact_NativeInvoker_CachesDelegatePerDelegateType()
    {
        var invoker = RuntimeCompiledArtifactTestFactory.CreateUnaryAddOneArtifact().GetNativeDelegateInvoker();

        var first = invoker.AsFunc<int, int>();
        var second = invoker.AsFunc<int, int>();

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void DynamicMethodArtifact_CreateSession_And_AsFunc_ProduceEquivalentResult()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateBinaryArtifact();
        var fromAsFunc = artifact.AsFunc<int, int, int>()(8, 3);

        var session = artifact.CreateSession();
        session.SetArgument("x", 8);
        session.SetArgument("y", 3);
        var fromSession = session.Run<int>();

        var fromInvoker = artifact.GetNativeDelegateInvoker().Invoke<int, int, int>(8, 3);

        Assert.That(fromSession, Is.EqualTo(fromAsFunc));
        Assert.That(fromInvoker, Is.EqualTo(fromAsFunc));
    }

    [Test]
    public void DynamicMethodArtifact_DeclaredBindingOrder_MatchesDelegateArgumentOrder()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateDeclaredOrderArtifact();

        var declaredNames = artifact.DeclaredBindings.Select(static binding => binding.Name).ToArray();
        var delegateResult = artifact.AsFunc<int, int, int>()(2, 9);

        Assert.That(declaredNames, Is.EqualTo(new[] { "left", "right" }));
        Assert.That(delegateResult, Is.EqualTo(29));
    }

    [Test]
    public void DynamicMethodArtifact_CreateExecutionBoundNativePointer_BindsEnvironmentFirstArgument()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateEnvironmentAndTwoArgumentsArtifact();
        var environment = new ExecutionEnvironment(artifact.DeclaredBindings);

        var nativePointer = artifact.CreateExecutionBoundNativePointer<int, int, int>(environment);
        var result = nativePointer.Invoke(4, 5);

        Assert.That(result, Is.EqualTo(45));
    }

    [Test]
    public void DynamicMethodArtifact_CreateExecutionBoundNativePointer_ZeroArguments_ReturnsExpectedValue()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateEnvironmentOnlyArtifact();
        var environment = new ExecutionEnvironment(artifact.DeclaredBindings);

        var nativePointer = artifact.CreateExecutionBoundNativePointer<int>(environment);
        var result = nativePointer.Invoke();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void DynamicMethodArtifact_CreateExecutionBoundNativePointer_OneArgument_ReturnsExpectedValue()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateEnvironmentAndOneArgumentArtifact();
        var environment = new ExecutionEnvironment(artifact.DeclaredBindings);

        var nativePointer = artifact.CreateExecutionBoundNativePointer<int, int>(environment);
        var result = nativePointer.Invoke(4);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void DynamicMethodArtifact_CreateExecutionBoundNativePointer_ProvidesNativeArgumentsToExternalRuntimeLoads()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateExternalRuntimeLoadThroughProviderArtifact();
        var environment = new ExecutionEnvironment(artifact.DeclaredBindings);

        var nativePointer = artifact.CreateExecutionBoundNativePointer<int, int, int>(environment);
        var result = nativePointer.Invoke(4, 5);

        Assert.That(result, Is.EqualTo(45));
    }

    [Test]
    public void DynamicMethodArtifact_CreateExecutionBoundNativePointer_SevenArguments_ReturnsExpectedValue()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateEnvironmentAndSevenArgumentsArtifact();
        var environment = new ExecutionEnvironment(artifact.DeclaredBindings);

        var nativePointer = artifact.CreateExecutionBoundNativePointer<int, int, int, int, int, int, int, int>(environment);
        var result = nativePointer.Invoke(1, 2, 3, 4, 5, 6, 7);

        Assert.That(result, Is.EqualTo(28));
    }

    [Test]
    public void DynamicMethodArtifact_CreateExecutionBoundNativePointer_MaxSupportedArguments_ReturnsExpectedValue()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateEnvironmentAndTenArgumentsArtifact();
        var environment = new ExecutionEnvironment(artifact.DeclaredBindings);

        var nativePointer = artifact.CreateExecutionBoundNativePointer<int, int, int, int, int, int, int, int, int, int, int>(environment);
        var result = nativePointer.Invoke(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

        Assert.That(result, Is.EqualTo(55));
    }

    [Test]
    public void DynamicMethodArtifact_CreateExecutionBoundNativePointer_WhenDeclaredBindingCountDoesNotMatch_Throws()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateEnvironmentAndTwoArgumentsArtifact();
        var environment = new ExecutionEnvironment(artifact.DeclaredBindings);

        var exception = Assert.Throws<InvalidOperationException>(() => artifact.CreateExecutionBoundNativePointer<int>(environment));

        Assert.That(exception!.Message, Does.Contain("requires exactly"));
    }

    [Test]
    public void DynamicMethodArtifact_CreateExecutionBoundNativePointer_WhenDeclaredBindingTypeDoesNotMatch_Throws()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateEnvironmentAndTwoArgumentsArtifact();
        var environment = new ExecutionEnvironment(artifact.DeclaredBindings);

        var exception = Assert.Throws<InvalidOperationException>(() => artifact.CreateExecutionBoundNativePointer<double, int, int>(environment));

        Assert.That(exception!.Message, Does.Contain("must have type"));
    }

    [Test]
    public void DynamicMethodInvoker_WhenParameterTypesDoNotMatch_ThrowsBeforeInvocation()
    {
        var artifact = RuntimeCompiledArtifactTestFactory.CreateEnvironmentAndTwoArgumentsArtifact();

        var exception = Assert.Throws<InvalidOperationException>(() => _ = new DynamicMethodInvoker<int, int, int>(artifact.CompilationOutput));

        Assert.That(exception!.Message, Does.Contain("parameter"));
    }
}