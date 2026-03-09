using System.Reflection.Emit;
using BasicCilCompiler.Execution;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.ExecutorWrapper;
using BasicCore.TranslatorWrapper;
using BytecodeDynamicMethodsCompiler.Compilers;
using DynamicMethodCalling.Core;

namespace Tests.Infrastructure;

[TestFixture]
public class CoreExecutionParameterFlowTests
{
    [Test]
    public void Should_KeepOrderedDictionaryParameterOrder_When_GeneratingDynamicMethod()
    {
        var method = GetExecutable(
            "a - b",
            new OrderedDictionary<string, Type>
            {
                ["a"] = typeof(int),
                ["b"] = typeof(int)
            });

        Assert.That(method.GetParameters().Select(x => x.ParameterType), Is.EqualTo(new[] { typeof(int), typeof(int) }));
    }

    [Test]
    public void Should_MapInvocationArgumentsByDeclaredOrder_When_UsingTypedInvoker()
    {
        var method = GetExecutable(
            "a - b",
            new OrderedDictionary<string, Type>
            {
                ["a"] = typeof(int),
                ["b"] = typeof(int)
            });

        var invoker = new DynamicMethodInvoker<int, int, int>(method);

        Assert.That(invoker.Invoke(7, 2), Is.EqualTo(5));
    }

    [Test]
    public void Should_RunWithoutParameters_When_CodeHasNoExternals()
    {
        var core = CreateDynamicMethodCore();

        var result = core.Run("40 + 2");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Should_RunWithOneParameter_When_RuntimeDictionaryProvided()
    {
        var core = CreateDynamicMethodCore();

        var result = core.Run("a + 2", new Dictionary<string, object>
        {
            ["a"] = 40
        });

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Should_RunWithTwoParameters_When_RuntimeDictionaryProvided()
    {
        var core = CreateDynamicMethodCore();

        var result = core.Run("a + b", new Dictionary<string, object>
        {
            ["a"] = 5,
            ["b"] = 7
        });

        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void Should_BeIsolatedAcrossRepeatedRunCalls_When_ReusingSameCore()
    {
        var core = CreateDynamicMethodCore();

        var first = core.Run("a + b", new Dictionary<string, object>
        {
            ["a"] = 5,
            ["b"] = 7
        });
        var second = core.Run("a + b", new Dictionary<string, object>
        {
            ["a"] = 10,
            ["b"] = 1
        });

        Assert.That(first, Is.EqualTo(12));
        Assert.That(second, Is.EqualTo(11));
    }

    [Test]
    public void Should_BeDeterministicAcrossRepeatedGetExecutableCalls_When_CodeAndBindingsAreSame()
    {
        var parameters = new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        };

        var first = GetExecutable("a + b", parameters);
        var second = GetExecutable("a + b", parameters);

        var firstInvoker = new DynamicMethodInvoker<int, int, int>(first);
        var secondInvoker = new DynamicMethodInvoker<int, int, int>(second);

        Assert.That(firstInvoker.Invoke(7, 15), Is.EqualTo(22));
        Assert.That(secondInvoker.Invoke(7, 15), Is.EqualTo(22));
    }

    [Test]
    public void Should_MatchRunAndCompiledExecutionSemantics_When_UsingEquivalentInputs()
    {
        const string code = "a + b";

        var runCore = CreateDynamicMethodCore();
        var runResult = runCore.Run(code, new Dictionary<string, object>
        {
            ["a"] = 7,
            ["b"] = 15
        });

        var method = GetExecutable(code, new OrderedDictionary<string, Type>
        {
            ["a"] = typeof(int),
            ["b"] = typeof(int)
        });
        var invoker = new DynamicMethodInvoker<int, int, int>(method);

        Assert.That(runResult, Is.EqualTo(22));
        Assert.That(invoker.Invoke(7, 15), Is.EqualTo(22));
    }

    private static DynamicMethod GetExecutable(string code, OrderedDictionary<string, Type> parameters)
    {
        var services = new ServiceCollection();
        services.AddWistServices(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);
        var provider = services.BuildServiceProvider();

        var giver = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();
        return giver.GetExecutable(code, parameters);
    }

    private static ICoreRunnable CreateDynamicMethodCore()
    {
        var services = new ServiceCollection();
        services.AddWistServices(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);
        var provider = services.BuildServiceProvider();

        var modules = provider.GetServices<IFrontendCoreModule>()
            .OrderBy(module => module.GetType().FullName, StringComparer.Ordinal)
            .ToList();
        var optimizers = provider.GetServices<IIRProcessingModule>()
            .OrderBy(module => module.GetType().FullName, StringComparer.Ordinal)
            .ToList();

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
}
