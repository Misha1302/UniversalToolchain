using BasicCore.Compilation;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Wist;

namespace Example.Scenarios;

public sealed class DslPricingCalculator : IDisposable
{
    private const string CompilerBackendName = "compiler";
    private const string InterpreterBackendName = "interpreter";

    private readonly WistDialectExecutionHost _host;

    public DslPricingCalculator()
    {
        _host = CreateHost();
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

    public void Dispose()
    {
        _host.Dispose();
    }

    private static WistDialectExecutionHost CreateHost()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var dialect = workflow.ComposeFile(GetDialectFilePath());

        if (!dialect.IsSuccess)
            Thrower.InvalidOpEx(dialect.ToDeterministicText());

        return workflow.CreateHost(dialect);
    }

    private static string GetDialectFilePath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "Dialects",
            "examples",
            "wist",
            "full-default-native",
            "dialect.wistdialect");
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings()
    {
        return new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };
    }

    private ICompiledArtifact<DynamicMethod> CompileWithCompiler(string formula)
    {
        var compiler = _host.GetArtifactCompiler<DynamicMethod>(CompilerBackendName);
        return compiler.Compile(formula, CreateDeclaredBindings());
    }
}
