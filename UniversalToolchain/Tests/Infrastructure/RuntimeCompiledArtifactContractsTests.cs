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
    public void CompiledArtifact_AsFuncAndNativeInvoker_ShouldExecuteHappyPath()
    {
        var artifact = CreateUnaryAddOneArtifact();

        Assert.That(artifact.AsFunc<int, int>()(41), Is.EqualTo(42));
        Assert.That(artifact.GetNativeDelegateInvoker().Invoke<int, int>(9), Is.EqualTo(10));
    }

    [Test]
    public void NativeDelegateInvoker_ShouldCacheDelegateInstancePerDelegateType()
    {
        var invoker = CreateUnaryAddOneArtifact().GetNativeDelegateInvoker();

        var first = invoker.AsFunc<int, int>();
        var second = invoker.AsFunc<int, int>();

        Assert.That(second, Is.SameAs(first));
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
            dynamicMethod);
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
