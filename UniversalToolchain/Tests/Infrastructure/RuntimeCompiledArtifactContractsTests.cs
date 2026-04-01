using System.Collections.Specialized;
using System.Reflection.Emit;
using DynamicMethodCalling;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace Tests.Infrastructure;

[TestFixture]
public class RuntimeCompiledArtifactContractsTests
{
    [Test]
    public void Compile_WithDeclaredBindings_ShouldPreserveBindingOrderInSlotsByName()
    {
        using var host = CreateHost();
        var compilerCore = GetCompilerCore(host);
        var declared = new OrderedDictionary<string, Type>
        {
            ["beta"] = typeof(object),
            ["alpha"] = typeof(object),
            ["gamma"] = typeof(object)
        };

        var artifact = compilerCore.Compile("alpha", declared);

        Assert.That(artifact.DeclaredBindings.Select(static b => b.Name), Is.EqualTo(new[] { "beta", "alpha", "gamma" }));
        Assert.That(artifact.SlotsByName["beta"], Is.EqualTo(0));
        Assert.That(artifact.SlotsByName["alpha"], Is.EqualTo(1));
        Assert.That(artifact.SlotsByName["gamma"], Is.EqualTo(2));
    }

    [Test]
    public void CompilerAndInterpreter_ShouldKeepDeclaredBindingsParity()
    {
        using var host = CreateHost();
        var declared = new OrderedDictionary<string, Type>
        {
            ["x"] = typeof(object),
            ["y"] = typeof(object)
        };

        var compilerArtifact = GetCompilerCore(host).Compile("x", declared);
        var interpreterArtifact = GetInterpreterCore(host).Compile("x", declared);

        Assert.That(compilerArtifact.DeclaredBindings.Select(static b => b.Name),
            Is.EqualTo(interpreterArtifact.DeclaredBindings.Select(static b => b.Name)));
        Assert.That(compilerArtifact.SlotsByName, Is.EqualTo(interpreterArtifact.SlotsByName));
    }

    [Test]
    public void DynamicMethodArtifact_AsFunc_SingleArgument_ReturnsExpectedValue()
    {
        var artifact = CreateUnaryAddOneArtifact();

        var result = artifact.AsFunc<int, int>()(41);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void DynamicMethodArtifact_AsFunc_TwoArguments_ReturnsExpectedValue()
    {
        var artifact = CreateBinaryArtifact();

        var result = artifact.AsFunc<int, int, int>()(6, 7);

        Assert.That(result, Is.EqualTo(67));
    }

    [Test]
    public void DynamicMethodArtifact_NativeInvoker_InvokeSingleArgument_ReturnsExpectedValue()
    {
        var artifact = CreateUnaryAddOneArtifact();

        var result = artifact.GetNativeDelegateInvoker().Invoke<int, int>(9);

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void DynamicMethodArtifact_NativeInvoker_InvokeTwoArguments_ReturnsExpectedValue()
    {
        var artifact = CreateBinaryArtifact();

        var result = artifact.GetNativeDelegateInvoker().Invoke<int, int, int>(4, 5);

        Assert.That(result, Is.EqualTo(45));
    }

    [Test]
    public void DynamicMethodArtifact_NativeInvoker_CachesDelegatePerDelegateType()
    {
        var invoker = CreateUnaryAddOneArtifact().GetNativeDelegateInvoker();

        var first = invoker.AsFunc<int, int>();
        var second = invoker.AsFunc<int, int>();

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void DynamicMethodArtifact_CreateSession_And_AsFunc_ProduceEquivalentResult()
    {
        var artifact = CreateBinaryArtifact();
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
        var artifact = CreateDeclaredOrderArtifact();

        var declaredNames = artifact.DeclaredBindings.Select(static binding => binding.Name).ToArray();
        var delegateResult = artifact.AsFunc<int, int, int>()(2, 9);

        Assert.That(declaredNames, Is.EqualTo(new[] { "left", "right" }));
        Assert.That(delegateResult, Is.EqualTo(29));
    }

    private static ICompiledArtifact<DynamicMethod> CreateUnaryAddOneArtifact()
    {
        var dynamicMethod = new DynamicMethod("AddOne", typeof(int), [typeof(int)]);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        return new CompiledArtifact<DynamicMethod>(
            "x + 1",
            [new ExternalBinding { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable }],
            dynamicMethod,
            new DynamicMethodExecutor());
    }

    private static ICompiledArtifact<DynamicMethod> CreateBinaryArtifact()
    {
        var dynamicMethod = new DynamicMethod("ConcatDecimalDigits", typeof(int), [typeof(int), typeof(int)]);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_S, 10);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        return new CompiledArtifact<DynamicMethod>(
            "x * 10 + y",
            [
                new ExternalBinding { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "y", Type = typeof(int), Kind = ExternalBindingKind.Variable }
            ],
            dynamicMethod,
            new DynamicMethodExecutor());
    }

    private static ICompiledArtifact<DynamicMethod> CreateDeclaredOrderArtifact()
    {
        var dynamicMethod = new DynamicMethod("DeclaredBindingOrder", typeof(int), [typeof(int), typeof(int)]);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_S, 10);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        return new CompiledArtifact<DynamicMethod>(
            "left * 10 + right",
            [
                new ExternalBinding { Name = "left", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "right", Type = typeof(int), Kind = ExternalBindingKind.Variable }
            ],
            dynamicMethod,
            new DynamicMethodExecutor());
    }

    private sealed class DynamicMethodExecutor : IExecutor<DynamicMethod>
    {
        public object? Execute(DynamicMethod compilation, IExecutionEnvironment environment)
        {
            var parameters = compilation.GetParameters();
            var args = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
                args[i] = environment.GetExternalValue(i);

            return compilation.Invoke(null, args);
        }
    }

    private static WistDialectExecutionHost CreateHost()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(
            """
            dialect RuntimeContracts
            use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
            backend compiler,interpreter
            """,
            "runtime-contracts-inline");

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        return workflow.CreateHost(composition);
    }

    private static BasicCoreImpl<DynamicMethod> GetCompilerCore(WistDialectExecutionHost host)
    {
        return host.GetCore("compiler") as BasicCoreImpl<DynamicMethod>
               ?? Thrower.InvalidOpEx<BasicCoreImpl<DynamicMethod>>("Compiler core must be BasicCoreImpl<DynamicMethod>.");
    }

    private static BasicCoreImpl<IAbstractIR> GetInterpreterCore(WistDialectExecutionHost host)
    {
        return host.GetCore("interpreter") as BasicCoreImpl<IAbstractIR>
               ?? Thrower.InvalidOpEx<BasicCoreImpl<IAbstractIR>>("Interpreter core must be BasicCoreImpl<IAbstractIR>.");
    }
}
