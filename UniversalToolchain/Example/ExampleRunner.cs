using UniversalToolchain.Dialects.Wist;

namespace Example;

public sealed class ExampleRunner : IDisposable
{
    private readonly WistDialectExecutionHost _host;

    public ExampleRunner()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        ServiceProvider? provider = services.BuildServiceProvider();
        try
        {
            var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
            var composition = workflow.ComposeText(
            """
            dialect ExampleNative
            use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,NativeTypes,Equality,Conditions,Loops,Scopes,Variables,Labels,InternalPreprocessorLexemes,CSharpInterop
            backend cil,interpreter
            """,
            "example-inline");

            if (!composition.IsSuccess)
                Thrower.InvalidOpEx(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

            var owner = provider;
            provider = null;
            _host = workflow.CreateHost(composition, new WistRuntimeServiceOptions(), owner);
        }
        finally
        {
            provider?.Dispose();
        }
    }

    public void Dispose() => _host.Dispose();

    public void RunInterpreter(string code)
    {
        Console.WriteLine("Runned: " + _host.Run(code, "interpreter"));
    }

    public void RunCompiled(string code, OrderedDictionary<string, Type> _)
    {
        Console.WriteLine("Result: " + _host.Run(code, "cil"));
    }
}
