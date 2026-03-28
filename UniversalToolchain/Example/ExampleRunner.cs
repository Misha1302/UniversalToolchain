namespace Example;

public class ExampleRunner
{
    private readonly ServiceProvider _provider;

    public ExampleRunner()
    {
        var services = new ServiceCollection();

        services.AddWistServices(options =>
            options.ArithmeticMode = ArithmeticMode.Native
        );

        // Add optional modules
        services.AddSingleton<IFrontendCoreModule>(new ExecutorDebugLoggerImpl());
        services.AddSingleton<IFrontendCoreModule>(new ParserConfigurationModuleImpl(ActionType.DumpConfiguration));
        services.AddSingleton<IFrontendCoreModule>(new LexerConfigurationModuleImpl(ActionType.DumpConfiguration));

        _provider = services.BuildServiceProvider();
    }

    public void RunInterpreter(string code)
    {
        var c = _provider.GetService<ICoreRunnable>().NotNull();
        Console.WriteLine("Runned: " + c.Run(code));
    }


    public void RunCompiled(string code, OrderedDictionary<string, Type> parameters)
    {
        var core = _provider.GetService<IExecutableGiver<DynamicMethod>>().NotNull();
        var method = core.GetExecutable(code, parameters);
        var fastCallable = new DynamicMethodInvoker<int, int, int>(method);

        Console.WriteLine("Result: " + fastCallable.Invoke(7, 15));
    }
}