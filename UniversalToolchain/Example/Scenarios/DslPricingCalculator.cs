using BasicCore.Compilation;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Wist;

namespace Example.Scenarios;

public sealed class DslPricingCalculator : IDisposable
{
    private const string DefaultDialectProfileName = "full-default-native";
    private const string CompilerBackendName = "compiler";
    private const string InterpreterBackendName = "interpreter";

    private readonly WistDialectExecutionHost _host;

    public DslPricingCalculator(string dialectProfileName = DefaultDialectProfileName)
    {
        _host = CreateHost(dialectProfileName);
    }

    public void Dispose()
    {
        _host.Dispose();
    }

    public double CalculateWithCompiler(string formula, double price, double fee)
    {
        var compiledArtifact = CompileWithCompiler(formula);
        var calculate = compiledArtifact.AsFunc<double, double, double>();

        return calculate(price, fee);
    }

    public double CalculateWithInterpreter(string formula, double price, double fee)
    {
        var interpreter = _host.GetArtifactCompiler<IAbstractIR>(InterpreterBackendName);
        var interpretedArtifact = interpreter.Compile(formula, CreateDeclaredBindings());
        var session = interpretedArtifact.CreateSession();

        session.SetArgument("price", price);
        session.SetArgument("fee", fee);

        return (double)session.Run().NotNull();
    }

    public double CalculateWithFastInvoker(string formula, double price, double fee)
    {
        var compiledArtifact = CompileWithCompiler(formula);
        var fastNativeInvoker = new DynamicMethodInvoker<double, double, double>(compiledArtifact.CompilationOutput);

        return fastNativeInvoker.Invoke(price, fee);
    }

    /// <summary>
    ///     Attempts to compile a pricing formula with the interpreter backend and preserves failure diagnostics.
    /// </summary>
    public CompilationAttemptResult TryCompileWithInterpreter(string formula)
    {
        try
        {
            var interpreter = _host.GetArtifactCompiler<IAbstractIR>(InterpreterBackendName);
            _ = interpreter.Compile(formula, CreateDeclaredBindings());

            return CompilationAttemptResult.Success();
        }
        catch (Exception exception)
        {
            return CompilationAttemptResult.Failure(exception);
        }
    }

    private static WistDialectExecutionHost CreateHost(string dialectProfileName)
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var dialect = workflow.ComposeFile(GetDialectFilePath(dialectProfileName));

        if (!dialect.IsSuccess)
            Thrower.InvalidOpEx(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(dialect)));

        return workflow.CreateHost(dialect);
    }

    private static string GetDialectFilePath(string dialectProfileName) =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Dialects",
            "examples",
            "wist",
            dialectProfileName,
            "dialect.wistdialect");

    private static OrderedDictionary<string, Type> CreateDeclaredBindings() =>
        new()
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };

    private ICompiledArtifact<DynamicMethod> CompileWithCompiler(string formula)
    {
        var compiler = _host.GetArtifactCompiler<DynamicMethod>(CompilerBackendName);
        return compiler.Compile(formula, CreateDeclaredBindings());
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