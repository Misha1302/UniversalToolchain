using AbstractIrConverters;
using System.Reflection;
using BasicCilCompiler.Execution;
using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicInterpreter;
using BytecodeDynamicMethodsCompiler.Compilers;
using DynamicMethodCalling.Core;

namespace Tests.Infrastructure;

internal static class ParameterTestHost
{
    public static IServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistServices(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);
        return services.BuildServiceProvider();
    }

    public static BasicCoreImpl<DynamicMethod> CreateDynamicCore(IServiceProvider provider)
    {
        var modules = provider.GetServices<IFrontendCoreModule>().ToList();
        var optimizers = provider.GetServices<IIRProcessingModule>().ToList();

        return new BasicCoreImpl<DynamicMethod>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => provider.GetRequiredService<AbstractMethodsCompilerImpl>(),
            () => new DynamicMethodExecutor(),
            modules,
            optimizers,
            []);
    }

    public static BasicCoreImpl<IAbstractIR> CreateInterpreterCore(IServiceProvider provider)
    {
        var modules = provider.GetServices<IFrontendCoreModule>().ToList();
        var optimizers = provider.GetServices<IIRProcessingModule>().ToList();

        return new BasicCoreImpl<IAbstractIR>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => provider.GetRequiredService<AbstractIrToAbstractIrStub>(),
            () => new InterpreterImpl(),
            modules,
            optimizers,
            []);
    }

    public static string ProgramFor(string expression) => $"""
        let result = {expression}
        result
        """;
}

[TestFixture]
public class RuntimeParameterHandlingTests
{
    [Test]
    public void Run_WithNoParameters_WorksForConstantExpression()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var result = core.Run(ParameterTestHost.ProgramFor("40 + 2"));

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Run_WithNullDictionary_BehavesSameAsNoArguments()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var result = core.Run(ParameterTestHost.ProgramFor("40 + 2"), null);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Run_WithEmptyDictionary_BehavesSameAsNoArguments()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var result = core.Run(ParameterTestHost.ProgramFor("40 + 2"), new Dictionary<string, object>());

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Run_WithOneParameter_BindsByName()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var result = core.Run(ParameterTestHost.ProgramFor("a + 3"), new Dictionary<string, object> { ["a"] = 5 });

        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void Run_WithTwoParameters_BindsBothValues()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var result = core.Run(ParameterTestHost.ProgramFor("a + b"), new Dictionary<string, object> { ["a"] = 5, ["b"] = 7 });

        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void Run_WithThreeParameters_BindsAllValues()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var result = core.Run(ParameterTestHost.ProgramFor("a * 100 + b * 10 + c"), new Dictionary<string, object>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        });

        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public void Run_WithRepeatedParameterReferences_UsesSameBoundValueEachTime()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var result = core.Run(ParameterTestHost.ProgramFor("a + a + b + a"), new Dictionary<string, object>
        {
            ["a"] = 4,
            ["b"] = 1
        });

        Assert.That(result, Is.EqualTo(13));
    }

    [Test]
    public void Run_WithDictionaryInsertionOrderDifferentFromUsage_BindsByNameNotPosition()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var result = core.Run(ParameterTestHost.ProgramFor("a * 10 + b"), new Dictionary<string, object>
        {
            ["b"] = 3,
            ["a"] = 4
        });

        Assert.That(result, Is.EqualTo(43));
    }

    [Test]
    public void Run_WithUnusedExtraRuntimeParameter_DoesNotAffectResult()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var result = core.Run(ParameterTestHost.ProgramFor("a + b"), new Dictionary<string, object>
        {
            ["a"] = 9,
            ["b"] = 1,
            ["unused"] = 123
        });

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Run_WithSimilarParameterNames_DoesNotCollide()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var result = core.Run(ParameterTestHost.ProgramFor("a + aa + a1 + arg + arg1"), new Dictionary<string, object>
        {
            ["a"] = 1,
            ["aa"] = 2,
            ["a1"] = 3,
            ["arg"] = 4,
            ["arg1"] = 5
        });

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Run_WithMissingRuntimeArgument_Throws()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        Assert.Throws<InvalidOperationException>(() => core.Run(ParameterTestHost.ProgramFor("a + b"), new Dictionary<string, object>
        {
            ["a"] = 10
        }));
    }

    [Test]
    public void Run_WithNullRuntimeValue_ThrowsNullReferenceException()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        Assert.Throws<NullReferenceException>(() => core.Run(ParameterTestHost.ProgramFor("a"), new Dictionary<string, object>
        {
            ["a"] = null!
        }));
    }
}

[TestFixture]
public class CompiledParameterHandlingTests
{
    [Test]
    public void GetExecutable_WithNoParameters_BuildsZeroArgMethod()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var method = core.GetExecutable(ParameterTestHost.ProgramFor("10 + 5"), new OrderedDictionary<string, Type>());
        var invoker = new DynamicMethodInvoker<int>(method);

        Assert.That(method.GetParameters(), Has.Length.EqualTo(0));
        Assert.That(invoker.Invoke(), Is.EqualTo(15));
    }

    [Test]
    public void GetExecutable_WithOneParameter_BuildsSingleArgMethod()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var method = core.GetExecutable(ParameterTestHost.ProgramFor("a + 1"), new OrderedDictionary<string, Type> { ["a"] = typeof(int) });
        var invoker = new DynamicMethodInvoker<int, int>(method);

        Assert.That(method.GetParameters().Select(x => x.ParameterType), Is.EqualTo(new[] { typeof(int) }));
        Assert.That(invoker.Invoke(41), Is.EqualTo(42));
    }

    [Test]
    public void GetExecutable_WithTwoParameters_BuildsTwoArgMethod()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var method = core.GetExecutable(ParameterTestHost.ProgramFor("a + b"), new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });
        var invoker = new DynamicMethodInvoker<int, int, int>(method);

        Assert.That(method.GetParameters().Select(x => x.ParameterType), Is.EqualTo(new[] { typeof(int), typeof(int) }));
        Assert.That(invoker.Invoke(7, 15), Is.EqualTo(22));
    }

    [Test]
    public void GetExecutable_WithThreeParameters_BuildsThreeArgMethod()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var method = core.GetExecutable(ParameterTestHost.ProgramFor("a * 100 + b * 10 + c"), new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int),
            ["c"] = typeof(int)
        });
        var invoker = new DynamicMethodInvoker<int, int, int, int>(method);

        Assert.That(invoker.Invoke(1, 2, 3), Is.EqualTo(123));
    }

    [Test]
    public void GetExecutable_WithUnusedDeclaredParameter_AddsParameterButDoesNotChangeSemantics()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var method = core.GetExecutable(ParameterTestHost.ProgramFor("a + b"), new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int),
            ["c"] = typeof(int)
        });
        var invoker = new DynamicMethodInvoker<int, int, int, int>(method);

        Assert.That(method.GetParameters(), Has.Length.EqualTo(3));
        Assert.That(invoker.Invoke(5, 7, 999), Is.EqualTo(12));
    }

    [Test]
    public void GetExecutable_WithMissingDeclaredParameter_Throws()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        Assert.Throws<InvalidOperationException>(() => core.GetExecutable(ParameterTestHost.ProgramFor("a + b"), new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int)
        }));
    }

    [Test]
    public void DynamicMethodInvoke_WithWrongArgumentCount_ThrowsTargetParameterCountException()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var method = core.GetExecutable(ParameterTestHost.ProgramFor("a + b"), new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });

        Assert.Throws<TargetParameterCountException>(() => method.Invoke(null, [1]));
    }

    [Test]
    public void DynamicMethodInvoke_WithWrongArgumentType_ThrowsArgumentException()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var method = core.GetExecutable(ParameterTestHost.ProgramFor("a + b"), new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });

        Assert.Throws<ArgumentException>(() => method.Invoke(null, ["bad", 2]));
    }

    [Test]
    public void GetExecutable_SameInputRepeatedly_ProducesDeterministicResults()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var parameters = new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        };

        var first = core.GetExecutable(ParameterTestHost.ProgramFor("a * 10 + b"), parameters);
        var second = core.GetExecutable(ParameterTestHost.ProgramFor("a * 10 + b"), parameters);

        var firstInvoker = new DynamicMethodInvoker<int, int, int>(first);
        var secondInvoker = new DynamicMethodInvoker<int, int, int>(second);

        Assert.That(firstInvoker.Invoke(4, 3), Is.EqualTo(43));
        Assert.That(secondInvoker.Invoke(4, 3), Is.EqualTo(43));
    }
}

[TestFixture]
public class ParameterOrderingAndConsistencyTests
{
    [Test]
    public void OrderedDictionaryOrder_ChangesCallableSignatureOrder()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var abc = core.GetExecutable(ParameterTestHost.ProgramFor("a * 100 + b * 10 + c"), new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int),
            ["c"] = typeof(int)
        });
        var cab = core.GetExecutable(ParameterTestHost.ProgramFor("a * 100 + b * 10 + c"), new OrderedDictionary<string, Type>
        {
            ["c"] = typeof(int),
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });

        var abcInvoker = new DynamicMethodInvoker<int, int, int, int>(abc);
        var cabInvoker = new DynamicMethodInvoker<int, int, int, int>(cab);

        Assert.That(abcInvoker.Invoke(1, 2, 3), Is.EqualTo(123));
        Assert.That(cabInvoker.Invoke(1, 2, 3), Is.EqualTo(231));
    }

    [Test]
    public void OrderedDictionaryOrder_PreservesParameterTypePositions()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var method = core.GetExecutable(ParameterTestHost.ProgramFor("a + b + c"), new OrderedDictionary<string, Type>
        {
            ["c"] = typeof(int),
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });

        Assert.That(method.GetParameters().Select(x => x.ParameterType), Is.EqualTo(new[] { typeof(int), typeof(int), typeof(int) }));
        Assert.That(method.GetParameters(), Has.Length.EqualTo(3));
    }

    [Test]
    public void RuntimeBinding_WithDifferentDictionaryOrders_ProducesSameResult()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        var code = ParameterTestHost.ProgramFor("a * 10 + b");

        var first = core.Run(code, new Dictionary<string, object> { ["a"] = 4, ["b"] = 3 });
        var second = core.Run(code, new Dictionary<string, object> { ["b"] = 3, ["a"] = 4 });

        Assert.That(first, Is.EqualTo(43));
        Assert.That(second, Is.EqualTo(43));
    }

    [Test]
    public void RuntimeVsCompiled_ForSimpleArithmetic_AreConsistent()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var runtimeCore = ParameterTestHost.CreateDynamicCore(provider!);
        var compiledCore = ParameterTestHost.CreateDynamicCore(provider!);

        var code = ParameterTestHost.ProgramFor("a + b * 2");

        var runtime = runtimeCore.Run(code, new Dictionary<string, object> { ["a"] = 5, ["b"] = 7 });
        var method = compiledCore.GetExecutable(code, new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });
        var compiled = new DynamicMethodInvoker<int, int, int>(method).Invoke(5, 7);

        Assert.That(runtime, Is.EqualTo(compiled));
    }

    [Test]
    public void RuntimeVsCompiled_ForRepeatedParameters_AreConsistent()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var runtimeCore = ParameterTestHost.CreateDynamicCore(provider!);
        var compiledCore = ParameterTestHost.CreateDynamicCore(provider!);

        var code = ParameterTestHost.ProgramFor("a + a + b + a");

        var runtime = runtimeCore.Run(code, new Dictionary<string, object> { ["a"] = 4, ["b"] = 1 });
        var method = compiledCore.GetExecutable(code, new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });
        var compiled = new DynamicMethodInvoker<int, int, int>(method).Invoke(4, 1);

        Assert.That(runtime, Is.EqualTo(compiled));
    }

    [Test]
    public void RuntimeVsCompiled_ForConditionExpression_AreConsistent()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var runtimeCore = ParameterTestHost.CreateDynamicCore(provider!);
        var compiledCore = ParameterTestHost.CreateDynamicCore(provider!);

        var code = """
            if a > b (
                a - b
            )
            else (
                b - a
            )
            """;

        var runtime = runtimeCore.Run(code, new Dictionary<string, object> { ["a"] = 3, ["b"] = 10 });
        var method = compiledCore.GetExecutable(code, new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });
        var compiled = new DynamicMethodInvoker<int, int, int>(method).Invoke(3, 10);

        Assert.That(runtime, Is.EqualTo(7));
        Assert.That(compiled, Is.EqualTo(7));
    }

    [Test]
    public void RuntimeVsCompiled_WithDeclarationOrderChanged_RemainsSemanticallyCorrect()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var runtimeCore = ParameterTestHost.CreateDynamicCore(provider!);
        var compiledCore = ParameterTestHost.CreateDynamicCore(provider!);

        var code = ParameterTestHost.ProgramFor("a * 100 + b * 10 + c");

        var runtime = runtimeCore.Run(code, new Dictionary<string, object>
        {
            ["a"] = 2,
            ["b"] = 3,
            ["c"] = 4
        });

        var method = compiledCore.GetExecutable(code, new OrderedDictionary<string, Type>
        {
            ["c"] = typeof(int),
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });
        var compiled = new DynamicMethodInvoker<int, int, int, int>(method).Invoke(4, 2, 3);

        Assert.That(runtime, Is.EqualTo(234));
        Assert.That(compiled, Is.EqualTo(234));
    }

}

[TestFixture]
public class PreparedExecutionParameterTests
{
    [Test]
    public void PrepareToRun_ThenRunPrepared_ExecutesWithoutParameters()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        ICoreOptimizedRunnable core = ParameterTestHost.CreateDynamicCore(provider!);

        core.PrepareToRun(ParameterTestHost.ProgramFor("40 + 2"));
        var result = core.RunPrepared();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void PrepareToRun_CalledTwice_UsesLatestPreparedProgram()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        ICoreOptimizedRunnable core = ParameterTestHost.CreateDynamicCore(provider!);

        core.PrepareToRun(ParameterTestHost.ProgramFor("1 + 2"));
        var first = core.RunPrepared();

        core.PrepareToRun(ParameterTestHost.ProgramFor("10 + 20"));
        var second = core.RunPrepared();

        Assert.That(first, Is.EqualTo(3));
        Assert.That(second, Is.EqualTo(30));
    }

    [Test]
    public void PrepareToRun_SameProgramRepeated_RunPreparedIsDeterministic()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        ICoreOptimizedRunnable core = ParameterTestHost.CreateDynamicCore(provider!);

        core.PrepareToRun(ParameterTestHost.ProgramFor("5 + 6"));
        var first = core.RunPrepared();
        var second = core.RunPrepared();
        var third = core.RunPrepared();

        Assert.That(first, Is.EqualTo(11));
        Assert.That(second, Is.EqualTo(11));
        Assert.That(third, Is.EqualTo(11));
    }

    [Test]
    public void PrepareToRun_WithDeclaredParameters_AndNoValues_UsesDefaultValuesOnExecution()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        ICoreOptimizedRunnable core = ParameterTestHost.CreateDynamicCore(provider!);

        core.PrepareToRun(ParameterTestHost.ProgramFor("a + b"), new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });

        Assert.That(core.RunPrepared(), Is.EqualTo(0));
    }

    [Test]
    public void PrepareToRunCompilationInput_WithValues_AllowsRunPreparedWithParameters()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        core.PrepareToRun(new CompilationInput
        {
            SourceText = ParameterTestHost.ProgramFor("a + b"),
            ExternalBindings =
            [
                new ExternalBinding { Name = "a", Type = typeof(int), Value = 7, Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "b", Type = typeof(int), Value = 15, Kind = ExternalBindingKind.Variable }
            ]
        });

        Assert.That(core.RunPrepared(), Is.EqualTo(22));
    }

    [Test]
    public void PrepareToRunCompilationInput_ReprepareWithDifferentBindings_DoesNotLeakState()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        core.PrepareToRun(new CompilationInput
        {
            SourceText = ParameterTestHost.ProgramFor("a + b"),
            ExternalBindings =
            [
                new ExternalBinding { Name = "a", Type = typeof(int), Value = 5, Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "b", Type = typeof(int), Value = 7, Kind = ExternalBindingKind.Variable }
            ]
        });
        var first = core.RunPrepared();

        core.PrepareToRun(new CompilationInput
        {
            SourceText = ParameterTestHost.ProgramFor("x * 10 + y"),
            ExternalBindings =
            [
                new ExternalBinding { Name = "x", Type = typeof(int), Value = 4, Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "y", Type = typeof(int), Value = 3, Kind = ExternalBindingKind.Variable }
            ]
        });
        var second = core.RunPrepared();

        Assert.That(first, Is.EqualTo(12));
        Assert.That(second, Is.EqualTo(43));
    }

    [Test]
    public void PrepareToRunCompilationInput_RepeatedRunPrepared_DoesNotMutateBoundExternalValues()
    {
        using var provider = ParameterTestHost.CreateProvider() as ServiceProvider;
        var core = ParameterTestHost.CreateDynamicCore(provider!);

        core.PrepareToRun(new CompilationInput
        {
            SourceText = ParameterTestHost.ProgramFor("a + b"),
            ExternalBindings =
            [
                new ExternalBinding { Name = "a", Type = typeof(int), Value = 2, Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "b", Type = typeof(int), Value = 9, Kind = ExternalBindingKind.Variable }
            ]
        });

        var first = core.RunPrepared();
        var second = core.RunPrepared();

        Assert.That(first, Is.EqualTo(11));
        Assert.That(second, Is.EqualTo(11));
    }
}
