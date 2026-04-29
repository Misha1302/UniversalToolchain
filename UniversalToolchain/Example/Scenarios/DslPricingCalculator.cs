using BasicCore.Compilation;
using BasicCore.Execution;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Presets;

namespace Example.Scenarios;

public sealed class DslPricingCalculator : IDisposable
{
    private const string CompilerBackendName = "compiler";
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
        var session = compiledArtifact.CreateSession();

        session.SetArgument("price", price);
        session.SetArgument("fee", fee);

        return (double)session.Run().NotNull();
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
        var environment = CreateExecutionEnvironment(compiledArtifact, price, fee);
        var fastNativeInvoker = new DynamicMethodInvoker<IExecutionEnvironment, double, double, double>(compiledArtifact.CompilationOutput);

        return fastNativeInvoker.Invoke(environment, price, fee);
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

    private static WistDialectExecutionHost CreateHost(WistShippedDialectPreset dialectPreset)
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var dialectFilePath = new WistShippedDialectFileResolver().Resolve(dialectPreset);
        var dialect = workflow.ComposeFile(dialectFilePath);

        if (!dialect.IsSuccess)
            Thrower.InvalidOpEx(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(dialect)));

        return workflow.CreateHost(dialect);
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings() =>
        new()
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };

    private static IExecutionEnvironment CreateExecutionEnvironment(ICompiledArtifact compiledArtifact, double price, double fee)
    {
        compiledArtifact = compiledArtifact.ArgNotNull();

        var environment = new ExecutionEnvironment(compiledArtifact.DeclaredBindings);
        environment.SetExternalValue(GetRequiredSlot(compiledArtifact, "price"), price);
        environment.SetExternalValue(GetRequiredSlot(compiledArtifact, "fee"), fee);

        return environment;
    }

    private static int GetRequiredSlot(ICompiledArtifact compiledArtifact, string name)
    {
        compiledArtifact = compiledArtifact.ArgNotNull();
        name = name.ArgNotNull();

        if (!compiledArtifact.SlotsByName.TryGetValue(name, out var slot))
            Thrower.Argument(nameof(name), $"Unknown argument name '{name}'.");

        return slot;
    }

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
