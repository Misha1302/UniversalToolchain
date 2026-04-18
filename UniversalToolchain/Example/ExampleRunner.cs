using UniversalToolchain.Dialects.Wist;

namespace Example;

public class ExampleRunner
{
    private readonly WistDialectExecutionHost _host;

    public ExampleRunner()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(
            """
            dialect ExampleNative
            use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,NativeTypes,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
            enable LocalVariablesOptimization
            backend compiler,interpreter
            """,
            "example-inline");

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        _host = workflow.CreateHost(composition);
    }

    public void RunInterpreter(string code)
    {
        Console.WriteLine("Runned: " + _host.Run(code, "interpreter"));
    }

    public void RunCompiled(string code, OrderedDictionary<string, Type> _)
    {
        Console.WriteLine("Result: " + _host.Run(code, "compiler"));
    }
}