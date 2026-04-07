using UniversalToolchain.Dialects.Wist;
using Tests.Infrastructure;

namespace Tests.Legacy;

// Temporary legacy adapter. Do not use in new tests.
[TestFixture]
// Legacy-only base for smoke/regression scenarios. New tests must use DialectTestHostInfrastructure + BackendParityInfrastructure.
[Obsolete("LegacyTestBase is temporary and allowed only for legacy smoke/parity-smoke scenarios. New tests must use DialectTestHostInfrastructure or BackendParityInfrastructure.")]
public abstract class LegacyTestBase
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
        return BackendValueNormalizer.ConvertTo<T>(result);
    }

    private static object? CastType(object value, Type t)
    {
        var normalized = BackendValueNormalizer.Normalize(value);

        if (normalized is null)
            return t == typeof(object) ? null : Thrower.InvalidCast<object?>($"Cannot convert test result from type {value.GetType()} to {t}.");

        if (t.IsInstanceOfType(normalized))
            return normalized;

        if (normalized is IConvertible convertible)
            return Convert.ChangeType(convertible, t);

        try
        {
            return Convert.ChangeType(normalized, t);
        }
        catch (Exception)
        {
            return Thrower.InvalidCast<object?>($"Cannot convert test result from type {value.GetType()} to {t}.");
        }
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
                                          use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
                                          enable LocalVariablesOptimization
                                          backend compiler,interpreter
                                          """;

    private const string NativeDialect = """
                                       dialect TestNative
                                       use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,NativeTypes,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
                                       enable LocalVariablesOptimization
                                       backend compiler,interpreter
                                       """;

    private sealed class DialectCoreRunnable(WistDialectExecutionHost host, string mode) : ICoreRunnable
    {
        public object? Run(string code, Dictionary<string, object>? args = null)
        {
            if (args != null && args.Count > 0)
                Thrower.InvalidOpEx("Parameterized execution is not supported by LegacyTestBase dialect adapter.");

            return host.Run(code, mode);
        }
    }
}
