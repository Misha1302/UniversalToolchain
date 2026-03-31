using UniversalToolchain.Dialects.Wist;

namespace Tests;

[TestFixture]
[Obsolete("TestBase is legacy and allowed only for smoke/parity-smoke scenarios. Use Tests.Infrastructure.DialectTestHostInfrastructure for new module isolation tests.")]
public abstract class TestBase
{
    protected const int CoresCount = 2;
    private IServiceProvider? _serviceProvider;
    private ArithmeticMode _arithmeticMode = ArithmeticMode.Universal;

    protected void SetArithmeticMode(ArithmeticMode mode)
    {
        _arithmeticMode = mode;
        _serviceProvider = null;
    }

    protected virtual IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICoreRunnable>(new DialectCoreRunnable(BuildHost(_arithmeticMode), "compiler"));
        services.AddSingleton<ICoreRunnable>(new DialectCoreRunnable(BuildHost(_arithmeticMode), "interpreter"));
        _serviceProvider = services.BuildServiceProvider();
        return _serviceProvider;
    }

    internal object ExecuteCode(string code)
    {
        if (_serviceProvider == null)
            BuildServiceProvider();

        var cores = _serviceProvider!.GetServices<ICoreRunnable>().ToList();
        var values = cores.Select(core => core.Run(code)).ToList();

        if (values.Any(x => x == null))
        {
            Assert.That(values.All(x => x == null));
            return null!;
        }

        var typedValues = values
            .Select(value => value!.GetType())
            .Select(type =>
            {
                try
                {
                    return values.Select(x => CastType(x!, type)!).ToList();
                }
                catch
                {
                    return null;
                }
            })
            .First(x => x != null)!;

        foreach (var value in typedValues)
            Assert.That(value, Is.EqualTo(typedValues[0]));

        return typedValues[0];
    }

    internal T ExecuteCode<T>(string code)
    {
        var result = ExecuteCode(code);
        return (T)CastType(result, typeof(T))!;
    }

    private static object? CastType(object value, Type t)
    {
        if (value.GetType() == t)
            return value;

        if (value is int i && t == typeof(bool))
            return i == 1;

        return Thrower.InvalidCast<object?>($"Cannot convert test result from type {value.GetType()} to {t}.");
    }

    protected T CreateCore<T>() where T : ICoreRunnable
    {
        if (_serviceProvider == null)
            BuildServiceProvider();

        return _serviceProvider!.GetServices<ICoreRunnable>()
            .OfType<T>()
            .FirstOrDefault()
            .NotNull($"Core of type {typeof(T).Name} not found");
    }

    private static WistDialectExecutionHost BuildHost(ArithmeticMode arithmeticMode)
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var dialectText = arithmeticMode == ArithmeticMode.Native ? NativeDialect : UniversalDialect;
        var composition = workflow.ComposeText(dialectText, "tests-inline");
        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        return workflow.CreateHost(composition);
    }

    private const string UniversalDialect = """
                                          dialect TestUniversal
                                          use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Strings,Identifier,Arithmetic,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
                                          enable LocalVariablesOptimization
                                          backend compiler,interpreter
                                          """;

    private const string NativeDialect = """
                                       dialect TestNative
                                       use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Strings,Identifier,Arithmetic,NativeMath,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
                                       enable LocalVariablesOptimization
                                       backend compiler,interpreter
                                       """;

    private sealed class DialectCoreRunnable(WistDialectExecutionHost host, string mode) : ICoreRunnable
    {
        public object? Run(string code, Dictionary<string, object>? args = null)
        {
            if (args != null && args.Count > 0)
                Thrower.InvalidOpEx("Parameterized execution is not supported by TestBase dialect adapter.");

            return host.Run(code, mode);
        }
    }
}