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
        var interpreter = _host.GetBackendSpecificArtifactCompiler<IAbstractIR>(InterpreterBackendName);
        var interpretedArtifact = interpreter.Compile(formula, CreateDeclaredBindings());
        var session = interpretedArtifact.CreateSession();

        session.SetArgument("price", price);
        session.SetArgument("fee", fee);

        return (double)session.Run().NotNull();
    }

    public double CalculateWithFastInvoker(string formula, double price, double fee)
    {
        var compiledArtifact = CompileWithCompiler(formula);
        var compilation = compiledArtifact.CompilationOutput;
        var method = compilation.Method;
        var parameters = method.GetParameters();
        var offset = 0;

        if (parameters.Length > offset && parameters[offset].ParameterType == typeof(ArtifactConstantPool))
            offset++;

        if (parameters.Length > offset && parameters[offset].ParameterType == typeof(IExecutionEnvironment))
        {
            var environment = new ExecutionEnvironment(compiledArtifact.DeclaredBindings);
            environment.SetExternalValue(compiledArtifact.SlotsByName["price"], price);
            environment.SetExternalValue(compiledArtifact.SlotsByName["fee"], fee);

            if (compilation.HasConstantPool)
            {
                var pooledEnvironmentInvoker = new DynamicMethodInvoker<ArtifactConstantPool, IExecutionEnvironment, double, double, double>(method);
                return pooledEnvironmentInvoker.Invoke(compilation.ConstantPool.NotNull(), environment, price, fee);
            }

            var environmentInvoker = new DynamicMethodInvoker<IExecutionEnvironment, double, double, double>(method);

            return environmentInvoker.Invoke(environment, price, fee);
        }

        if (compilation.HasConstantPool)
        {
            var pooledInvoker = new DynamicMethodInvoker<ArtifactConstantPool, double, double, double>(method);
            return pooledInvoker.Invoke(compilation.ConstantPool.NotNull(), price, fee);
        }

        var rawFastInvoker = new DynamicMethodInvoker<double, double, double>(method);

        return rawFastInvoker.Invoke(price, fee);
    }

    /// <summary>
    ///     Attempts to compile a pricing formula with the interpreter backend and preserves failure diagnostics.
    /// </summary>
    public CompilationAttemptResult TryCompileWithInterpreter(string formula)
    {
        try
        {
            var interpreter = _host.GetBackendSpecificArtifactCompiler<IAbstractIR>(InterpreterBackendName);
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

    private ICompiledArtifact<CilCompilationOutput> CompileWithCompiler(string formula)
    {
        var compiler = _host.GetBackendSpecificArtifactCompiler<CilCompilationOutput>(CompilerBackendName);
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
