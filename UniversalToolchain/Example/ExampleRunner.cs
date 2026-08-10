using UniversalToolchain.Wist;
using UniversalToolchain.Wist.LanguagePack;

namespace Example;

public sealed class ExampleRunner : IDisposable
{
    private readonly WistEngine _interpreter;
    private readonly WistEngine _cil;

    public ExampleRunner()
    {
        _interpreter = WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset(WistLanguageDefinitions.FullDefaultNativeId),
            BackendId = "interpreter"
        });
        _cil = WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset(WistLanguageDefinitions.FullDefaultNativeId),
            BackendId = "cil"
        });
    }

    public void Dispose()
    {
        _cil.Dispose();
        _interpreter.Dispose();
    }

    public void RunInterpreter(string code) =>
        Console.WriteLine("Runned: " + _interpreter.Evaluate<object?>(code));

    public void RunCompiled(string code, OrderedDictionary<string, Type> _) =>
        Console.WriteLine("Result: " + _cil.Evaluate<object?>(code));
}
