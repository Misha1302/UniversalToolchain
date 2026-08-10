using UniversalToolchain.Wist;
using UniversalToolchain.Wist.LanguagePack;

namespace Example.Scenarios;

public sealed class DslPricingCalculator : IDisposable
{
    private readonly WistEngine _compiler;
    private readonly WistEngine _interpreter;

    public DslPricingCalculator()
        : this(WistLanguageDefinitions.FullDefaultNativeId)
    {
    }

    public DslPricingCalculator(string presetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(presetId);
        _compiler = WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset(presetId),
            BackendId = "cil"
        });
        _interpreter = WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset(presetId),
            BackendId = "interpreter"
        });
    }

    public void Dispose()
    {
        _interpreter.Dispose();
        _compiler.Dispose();
    }

    public double CalculateWithCompiler(string formula, double price, double fee)
    {
        var program = _compiler.Compile<Func<double, double, double>>(formula, "price", "fee");
        return program.CompiledDelegate(price, fee);
    }

    public double CalculateWithInterpreter(string formula, double price, double fee)
    {
        var program = _interpreter.Compile<Func<double, double, double>>(formula, "price", "fee");
        return program.CompiledDelegate(price, fee);
    }

    public double CalculateWithFastInvoker(string formula, double price, double fee) =>
        CalculateWithCompiler(formula, price, fee);

    public CompilationAttemptResult TryCompileWithInterpreter(string formula)
    {
        var result = _interpreter.TryCompile<Func<double, double, double>>(formula, "price", "fee");
        return result.IsSuccess
            ? CompilationAttemptResult.Success()
            : CompilationAttemptResult.Failure(result.Exception ?? new InvalidOperationException("Wist compilation failed."));
    }

    public sealed record CompilationAttemptResult(bool IsSuccess, string? ErrorMessage, Exception? Exception)
    {
        public static CompilationAttemptResult Success() => new(true, null, null);
        public static CompilationAttemptResult Failure(Exception exception) => new(false, exception.Message, exception);
    }
}
