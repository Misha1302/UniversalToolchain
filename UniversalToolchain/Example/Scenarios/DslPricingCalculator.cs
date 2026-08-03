using BasicCore.Compilation;
using BasicCore.Execution;
using System.Reflection;
using System.Reflection.Emit;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Presets;

namespace Example.Scenarios;

public sealed class DslPricingCalculator : IDisposable
{
    private const string CilBackendName = "cil";
    private const string InterpreterBackendName = "interpreter";

    private readonly WistDialectExecutionHost _host;

    /// <summary>
    ///     Creates a calculator that uses the default native shipped Wist dialect preset for the example.
    /// </summary>
    public DslPricingCalculator()
        : this(WistShippedDialectPresets.FullDefaultNative)
    {
    }

    /// <summary>
    ///     Creates a calculator that uses the provided shipped Wist dialect preset.
    /// </summary>
    public DslPricingCalculator(WistShippedDialectPreset dialectPreset)
    {
        _host = CreateHost(dialectPreset);
    }

    public void Dispose()
    {
        _host.Dispose();
    }

    public double CalculateWithCompiler(string formula, double price, double fee)
    {
        var compiledArtifact = CompileWithCompiler(formula);
        return ConvertResultToDouble(_host.Run(compiledArtifact, CreateArguments(price, fee)));
    }

    public double CalculateWithInterpreter(string formula, double price, double fee)
    {
        var interpretedArtifact = _host.Compile(formula, CreateDeclaredBindings(), InterpreterBackendName);
        return ConvertResultToDouble(_host.Run(interpretedArtifact, CreateArguments(price, fee)));
    }

    public double CalculateWithFastInvoker(string formula, double price, double fee)
    {
        var compiledArtifact = CompileWithCompiler(formula);
        var compilation = GetCompilationOutput(compiledArtifact);
        var method = compilation.GetType()
                         .GetProperty("Method", BindingFlags.Instance | BindingFlags.Public)?
                         .GetValue(compilation) as DynamicMethod
                     ?? Thrower.InvalidOpEx<DynamicMethod>(
                         $"Compilation output '{compilation.GetType().FullName}' does not expose a DynamicMethod.");
        var constantPool = compilation.GetType()
            .GetProperty("ConstantPool", BindingFlags.Instance | BindingFlags.Public)?
            .GetValue(compilation);
        var parameters = method.GetParameters();
        if (constantPool is null &&
            parameters.Select(static parameter => parameter.ParameterType)
                .SequenceEqual([typeof(double), typeof(double)]))
        {
            return method.CreateDelegate<Func<double, double, double>>()(price, fee);
        }

        return ConvertResultToDouble(_host.Run(compiledArtifact, CreateArguments(price, fee)));
    }

    /// <summary>
    ///     Attempts to compile a pricing formula with the interpreter backend and preserves failure diagnostics.
    /// </summary>
    public CompilationAttemptResult TryCompileWithInterpreter(string formula)
    {
        try
        {
            _ = _host.Compile(formula, CreateDeclaredBindings(), InterpreterBackendName);

            return CompilationAttemptResult.Success();
        }
        catch (Exception exception)
        {
            return CompilationAttemptResult.Failure(exception);
        }
    }

    private static WistDialectExecutionHost CreateHost(WistShippedDialectPreset dialectPreset)
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        ServiceProvider? provider = services.BuildServiceProvider();
        try
        {
            var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
            var dialectFilePath = new WistShippedDialectFileResolver().Resolve(dialectPreset);
            var dialect = workflow.ComposeFile(dialectFilePath);

            if (!dialect.IsSuccess)
                Thrower.InvalidOpEx(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(dialect)));

            var owner = provider;
            provider = null;
            return workflow.CreateHost(dialect, new WistRuntimeServiceOptions(), owner);
        }
        finally
        {
            provider?.Dispose();
        }
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings() =>
        new()
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };

    private static IReadOnlyDictionary<string, object?> CreateArguments(double price, double fee) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["price"] = price,
            ["fee"] = fee
        };

    private ICompiledArtifact CompileWithCompiler(string formula) =>
        _host.Compile(formula, CreateDeclaredBindings(), CilBackendName);

    private static object GetCompilationOutput(ICompiledArtifact artifact) =>
        artifact.GetType()
            .GetProperty("CompilationOutput", BindingFlags.Instance | BindingFlags.Public)?
            .GetValue(artifact)
        ?? Thrower.InvalidOpEx<object>(
            $"Artifact type '{artifact.GetType().FullName}' does not expose a non-null compilation output.");

    private static double ConvertResultToDouble(object? value)
    {
        value = value.NotNull();
        if (value is IConvertible convertible)
            return convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture);

        var wrapped = value.GetType()
            .GetMethod("GetValue", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null)?
            .Invoke(value, null);
        return Convert.ToDouble(wrapped.NotNull(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Describes the result of a pricing formula compilation attempt.
    /// </summary>
    public sealed record CompilationAttemptResult(bool IsSuccess, string? ErrorMessage, Exception? Exception)
    {
        public static CompilationAttemptResult Success() => new(true, null, null);

        public static CompilationAttemptResult Failure(Exception exception) => new(false, exception.Message, exception);
    }
}
